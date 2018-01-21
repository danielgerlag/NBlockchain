using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBlockchain.Interfaces;
using NBlockchain.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System.Diagnostics;

namespace NBlockchain.Services.Net
{
    public class TcpPeerNetwork : IPeerNetwork, IDisposable
    {
        //TODO: break this class up into smaller pieces
        private const int TargetOutgoingCount = 8;
        private readonly uint _port;
        private readonly INatTraversal _natTraversal;
        private string _serviceId = "BC";
        private int _version = 1;

        private readonly IReceiver _reciever;

        private readonly IBlockRepository _blockRepository;
        private readonly IEnumerable<IPeerDiscoveryService> _discoveryServices;
        private readonly ILogger _logger;
        private readonly IOwnAddressResolver _ownAddressResolver;
        private readonly IUnconfirmedTransactionPool _unconfirmedTransactionPool;

        private readonly ConcurrentQueue<KnownPeer> _peerRoundRobin = new ConcurrentQueue<KnownPeer>();

        private readonly List<PeerConnection> _peerConnections = new List<PeerConnection>();
        private CancellationTokenSource _cancelTokenSource;
        private TcpListener _listener;

        private readonly AutoResetEvent _peerEvent = new AutoResetEvent(true);
        private readonly AutoResetEvent _connectOutEvent = new AutoResetEvent(true);

        private Timer _sharePeersTimer;
        private Timer _discoveryTimer;

        private string _internalConnectionString;
        private string _externalConnectionString;

        private object _duplicateLock = new object();

        public Guid NodeId { get; private set; }

        public TcpPeerNetwork(uint port, INatTraversal natTraversal, IBlockRepository blockRepository, IEnumerable<IPeerDiscoveryService> discoveryServices, ILoggerFactory loggerFactory, IOwnAddressResolver ownAddressResolver, IUnconfirmedTransactionPool unconfirmedTransactionPool, IReceiver reciever)
        {
            _port = port;
            _natTraversal = natTraversal;
            _reciever = reciever;
            _logger = loggerFactory.CreateLogger<TcpPeerNetwork>();
            _blockRepository = blockRepository;
            _discoveryServices = discoveryServices;
            _ownAddressResolver = ownAddressResolver;
            _unconfirmedTransactionPool = unconfirmedTransactionPool;
            NodeId = Guid.NewGuid();
        }

        private ICollection<PeerConnection> GetActivePeers()
        {
            _peerEvent.WaitOne();
            try
            {
                return new List<PeerConnection>(_peerConnections);
            }
            finally
            {
                _peerEvent.Set();
            }
        }

        public void Open()
        {
            _cancelTokenSource = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Any, (int)_port);
            _listener.Start();

            Task.Factory.StartNew(async () =>
            {
                try
                {
                    while (!_cancelTokenSource.IsCancellationRequested)
                    {
                        var client = await _listener.AcceptTcpClientAsync();
                        _logger.LogDebug($"Client connected - {client.Client.RemoteEndPoint}");
                        var peer = new PeerConnection(_serviceId, _version, NodeId, client);
                        AttachEventHandlers(peer);
                        _peerEvent.WaitOne();
                        try
                        {
                            _peerConnections.Add(peer);
                            peer.Run();
                        }
                        finally
                        {
                            _peerEvent.Set();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error listening - {ex.Message}");
                }
            });

            DiscoverOwnConnectionStrings();
            AdvertiseToPeers();

            _discoveryTimer = new Timer(async (state) =>
            {
                await DiscoverPeers();
            }, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30));

            _sharePeersTimer = new Timer(SharePeers, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));


            Task.Factory.StartNew(async () =>
            {
                while (!_cancelTokenSource.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMinutes(5));
                    var peers = GetActivePeers();
                    _logger.LogInformation($"Check in - Outgoing peers: {peers.Count(x => x.Outgoing)}");
                    _logger.LogInformation($"Check in - Incoming peers: {peers.Count(x => !x.Outgoing)}");

                    //var process = Process.GetCurrentProcess();

                    //_logger.LogInformation($"Check in - Thread count: {process.Threads.Count}");
                    //_logger.LogInformation($"Check in - Working set: {process.WorkingSet64}");
                    //_logger.LogInformation($"Check in - PrivateMemorySize: {process.PrivateMemorySize64}");

                }
            });
        }

        private void AttachEventHandlers(PeerConnection peer)
        {
            peer.OnReceiveMessage += Peer_OnReceiveMessage;
            peer.OnDisconnect += Peer_OnDisconnect;
            peer.OnPeerException += Peer_OnPeerException;
            peer.OnUnresponsive += Peer_OnUnresponsive;
            peer.OnIdentify += Peer_OnIdentify;
        }

        private void Peer_OnIdentify(PeerConnection sender)
        {
            _logger.LogInformation($"Peer identify {sender.RemoteEndPoint} - {sender.RemoteId}");

            if (!string.IsNullOrEmpty(sender.ConnectionString))
            {
                var peers = _peerRoundRobin.Where(x => x.ConnectionString == sender.ConnectionString);
                foreach (var peer in peers)
                    peer.NodeId = sender.RemoteId;
            }

            //remove duplicate connections
            lock (_duplicateLock)
            {
                var peers = GetActivePeers();
                foreach (var peer in peers.Where(x => x.RemoteId == sender.RemoteId && x != sender))
                {
                    peer.Disconnect();
                    peer.Close();
                }
            }

            //remove connection to self
            if (sender.RemoteId == NodeId)
            {
                if (!string.IsNullOrEmpty(sender.ConnectionString))
                {
                    var selfs = _peerRoundRobin.Where(x => x.ConnectionString == sender.ConnectionString);
                    foreach (var self in selfs)
                        self.IsSelf = true;
                }
                sender.Disconnect();
                sender.Close();
            }
        }

        private void Peer_OnUnresponsive(PeerConnection sender)
        {
            _logger.LogInformation($"Unresponsive peer {sender.RemoteEndPoint}");
        }

        private void Peer_OnPeerException(PeerConnection sender, Exception exception)
        {
            _logger.LogError($"Peer exception {sender.RemoteEndPoint} - {exception.Message}");
        }

        private async void Peer_OnReceiveMessage(PeerConnection sender, byte command, byte[] data)
        {
            try
            {
                switch (command)
                {
                    case Commands.Block:
                        await ProcessBlock(data, sender.RemoteId, false);
                        break;
                    case Commands.BlockRequest:
                        await ProcessBlockRequest(data, false, sender);
                        break;
                    case Commands.NextBlockRequest:
                        await ProcessBlockRequest(data, true, sender);
                        break;
                    case Commands.BlockHeightRequest:
                        await ProcessBlockRequest(BitConverter.ToUInt32(data, 0), sender);
                        break;
                    case Commands.PeerShare:
                        if (IsSharablePeer(Encoding.UTF8.GetString(data)))
                            AddPeer(new KnownPeer() { ConnectionString = Encoding.UTF8.GetString(data) });
                        break;
                    case Commands.Tail:
                        await ProcessBlock(data, sender.RemoteId, true);
                        break;
                    case Commands.Txn:
                        await ProcessTransaction(data, sender.RemoteId);
                        break;
                    case Commands.TxnRequest:
                        ProcessTxnRequest(sender);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error procssing command {command} - {ex.Message}");
            }
        }

        private async Task ProcessBlockRequest(byte[] blockId, bool next, PeerConnection peer)
        {
            Block block = null;
            if (next)
                block = await _blockRepository.GetNextBlock(blockId);
            else
                block = await _blockRepository.GetBlock(blockId);

            if (block != null)
            {
                _logger.LogInformation($"Responding to block request {next} {BitConverter.ToString(blockId)}");
                var data = SerializeObject(block);
                SendBlock(peer, data, false);
            }
            else
            {
                _logger.LogInformation($"Unable to respond to block request {next} {BitConverter.ToString(blockId)}");
            }
        }

        private async Task ProcessBlockRequest(uint height, PeerConnection peer)
        {
            var header = await _blockRepository.GetPrimaryHeader(height);

            if (header != null)
            {
                _logger.LogInformation($"Responding to block request {height} with {BitConverter.ToString(header.BlockId)}");
                var block = await _blockRepository.GetBlock(header.BlockId);
                var data = SerializeObject(block);
                SendBlock(peer, data, false);
            }
            else
            {
                _logger.LogInformation($"Unable to respond to block request {height}");
            }
        }

        private async Task ProcessBlock(byte[] data, Guid originId, bool tip)
        {
            var block = DeserializeObject<Block>(data);

            _logger.LogDebug($"Recv block {BitConverter.ToString(block.Header.BlockId)} from {originId}");

            var result = PeerDataResult.Ignore;
            result = await _reciever.RecieveBlock(block);

            if ((tip) && (result == PeerDataResult.Relay))
            {
                var relayTask = Task.Factory.StartNew(() =>
                {
                    var peerList = GetActivePeers().Where(x => x.RemoteId != originId);
                    Parallel.ForEach(peerList, peer =>
                    {
                        SendBlock(peer, data, tip);
                    });
                });
            }

            if (result == PeerDataResult.Demerit)
            {
                PeerConnection peer = GetActivePeers().FirstOrDefault(x => x.RemoteId == originId);
                if (peer != null)
                    peer.DemeritPoints++;
            }
        }

        private async Task ProcessTransaction(byte[] data, Guid originId)
        {
            var txn = DeserializeObject<Transaction>(data);
            var result = await _reciever.RecieveTransaction(txn);

            if (result == PeerDataResult.Relay)
            {
                var relayTask = Task.Factory.StartNew(() =>
                {
                    var peerList = GetActivePeers().Where(x => x.RemoteId != originId);
                    Parallel.ForEach(peerList, peer =>
                    {
                        SendTxn(peer, data);
                    });
                });
            }

            if (result == PeerDataResult.Demerit)
            {
                PeerConnection peer = GetActivePeers().FirstOrDefault(x => x.RemoteId == originId);
                if (peer != null)
                    peer.DemeritPoints++;
            }
        }

        private void ProcessTxnRequest(PeerConnection peer)
        {
            var txns = _unconfirmedTransactionPool.Get;

            foreach (var txn in txns)
            {
                var data = SerializeObject(txn);
                SendTxn(peer, data);
            }
        }

        private void Peer_OnDisconnect(PeerConnection sender)
        {
            _peerEvent.WaitOne();
            try
            {
                _logger.LogInformation($"Peer disconnect {sender.RemoteId} {sender.RemoteEndPoint}");
                _peerConnections.Remove(sender);
                sender.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Disconnect error ({sender.RemoteId}) - {ex.Message}");
            }
            finally
            {
                _peerEvent.Set();
            }
        }

        private async Task OnboardPeer(string connStr)
        {
            try
            {
                var peer = new PeerConnection(_serviceId, _version, NodeId);
                await peer.Connect(connStr);
                AttachEventHandlers(peer);
                _peerEvent.WaitOne();
                try
                {
                    _peerConnections.Add(peer);
                    peer.Run();
                }
                finally
                {
                    _peerEvent.Set();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error connecting to {connStr} - {ex.Message}");
                var peers = _peerRoundRobin.Where(x => x.ConnectionString == connStr);
                foreach (var peer in peers)
                    peer.Unreachable = true;
            }
        }

        public void Close()
        {
            _discoveryTimer.Dispose();
            _sharePeersTimer.Dispose();
            _listener?.Stop();

            foreach (var peer in GetActivePeers())
            {
                peer.Disconnect();
            }
        }

        public async Task DiscoverPeers()
        {
            Parallel.ForEach(_discoveryServices, async discovery =>
            {
                try
                {
                    var newPeers = await discovery.DiscoverPeers();
                    foreach (var np in newPeers)
                        AddPeer(np);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
            });
            await ConnectOut();
        }

        private void AddPeer(KnownPeer newPeer)
        {
            if (_peerRoundRobin.All(x => x.ConnectionString != newPeer.ConnectionString))
                _peerRoundRobin.Enqueue(newPeer);
        }

        private async void SharePeers(object state)
        {
            foreach (var ds in _discoveryServices)
                await ds.SharePeers(_peerRoundRobin.ToList());
        }

        private async Task ConnectOut()
        {
            var activePeers = GetActivePeers();
            var peersOut = activePeers.Where(x => x.Outgoing).ToList();
            var target = (TargetOutgoingCount - peersOut.Count());
            if (target <= 0)
                return;

            var actual = 0;
            var counter = 0;

            _connectOutEvent.WaitOne();
            try
            {
                while ((actual < target) && (counter < _peerRoundRobin.Count))
                {
                    counter++;

                    if (!_peerRoundRobin.TryDequeue(out var kp))
                        continue;

                    _peerRoundRobin.Enqueue(kp);

                    if (kp.IsSelf)
                        continue;

                    if (peersOut.Any(x => x.ConnectionString == kp.ConnectionString))
                        continue;

                    if (activePeers.Any(x => x.RemoteId == kp.NodeId))
                        continue;

                    _logger.LogInformation($"Connecting to {kp.ConnectionString}");
                    await OnboardPeer(kp.ConnectionString);
                    actual++;
                }
            }
            finally
            {
                _connectOutEvent.Set();
            }
        }

        public void BroadcastTail(Block block)
        {
            var data = SerializeObject(block);
            var peers = GetActivePeers().Where(x => x.RemoteId != NodeId);

            Task.Factory.StartNew(() =>
            {
                Parallel.ForEach(peers, peer =>
                {
                    SendBlock(peer, data, true);
                });
            });
        }

        private void SendBlock(PeerConnection peer, byte[] data, bool tail)
        {
            try
            {
                peer.Send(tail ? Commands.Tail : Commands.Block, data);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending block {ex.Message}");
            }
        }

        public void BroadcastTransaction(Transaction transaction)
        {
            var data = SerializeObject(transaction);

            Task.Factory.StartNew(() =>
            {
                var peers = GetActivePeers().Where(x => x.RemoteId != NodeId);
                Parallel.ForEach(peers, peer =>
                {
                    SendTxn(peer, data);
                });
            });
        }

        private void SendTxn(PeerConnection peer, byte[] data)
        {
            try
            {
                peer.Send(Commands.Txn, data);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending txn {ex.Message}");
            }
        }

        public void RequestNextBlock(byte[] blockId)
        {
            LoadBalancedRequest((peer) =>
            {
                _logger.LogInformation($"Requesting next block {BitConverter.ToString(blockId)} from peer {peer.RemoteId}");
                peer.Send(Commands.NextBlockRequest, blockId);
                return Task.CompletedTask;
            },
            async () => (await _blockRepository.GetBlockHeader(blockId)) != null);
        }

        public void RequestBlock(byte[] blockId)
        {
            LoadBalancedRequest((peer) =>
            {
                _logger.LogInformation($"Requesting block {BitConverter.ToString(blockId)} from peer {peer.RemoteId}");
                peer.Send(Commands.BlockRequest, blockId);
                return Task.CompletedTask;
            },
            async () => (await _blockRepository.GetBlockHeader(blockId)) != null);
        }

        public void RequestBlockByHeight(uint height)
        {
            LoadBalancedRequest((peer) =>
            {
                _logger.LogInformation($"Requesting block {height} from peer {peer.RemoteId}");
                peer.Send(Commands.BlockHeightRequest, BitConverter.GetBytes(height));
                return Task.CompletedTask;
            },
            async () => (await _blockRepository.GetPrimaryHeader(height)) != null);
        }

        public void LoadBalancedRequest(Func<PeerConnection, Task> requestAction, Func<Task<bool>> resolveAction)
        {
            Task.Factory.StartNew(async () =>
            {
                var peers = GetActivePeers()
                    .Where(x => x.RemoteId != NodeId && x.LastContact.HasValue)
                    .Where(x => x.LastContact > (DateTime.Now.AddMinutes(-20)))
                    .OrderBy(x => x.RequestCount);
                //TODO: round robin
                foreach (var peer in peers)
                {
                    peer.RequestCount++;
                    await requestAction(peer);
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    if (await resolveAction())
                        return;
                }
            });
        }
        public void Dispose()
        {

        }

        private void AdvertiseToPeers()
        {
            foreach (var ds in _discoveryServices)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        if (_internalConnectionString != null)
                            ds.AdvertiseLocal(_internalConnectionString);

                        if (_externalConnectionString != null)
                            ds.AdvertiseGlobal(_externalConnectionString);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                    }
                });
        }

        private void DiscoverOwnConnectionStrings()
        {
            var ownAddress = _ownAddressResolver.ResolvePreferredLocalAddress();
            if (ownAddress != null)
                _internalConnectionString = $"tcp://{ownAddress}:{_port}";

            var externalConnectionString = _natTraversal.ConfigureNatTraversal(ownAddress, (int)_port);
            if (externalConnectionString != null)
                _externalConnectionString = $"tcp://{externalConnectionString}";
        }

        private static byte[] SerializeObject(object data)
        {
            using (var bw = new MemoryStream())
            {
                var writer = new BsonDataWriter(bw);
                var serializer = new JsonSerializer();
                serializer.TypeNameHandling = TypeNameHandling.Objects;
                serializer.Serialize(writer, data);
                writer.Close();
                bw.TryGetBuffer(out var result);
                return result.Array;
            }
        }

        private static T DeserializeObject<T>(byte[] bson)
        {
            using (var ms = new MemoryStream(bson))
            {
                var bdr = new BsonDataReader(ms);
                var serializer = new JsonSerializer();
                serializer.TypeNameHandling = TypeNameHandling.Objects;
                var result = serializer.Deserialize<T>(bdr);
                bdr.Close();
                return result;
            }
        }

        private static bool IsSharablePeer(string connectionUri)
        {
            var uri = new Uri(connectionUri);
            switch (uri.HostNameType)
            {
                case UriHostNameType.Dns:
                    var ipAddr = Dns.GetHostAddressesAsync(uri.DnsSafeHost).Result;
                    if (ipAddr.Length == 0)
                        return false;
                    return IsSharablePeer($"{uri.Scheme}://{ipAddr[0]}:{uri.Port}");
                case UriHostNameType.IPv4:
                    var ip = IPAddress.Parse(uri.Host).GetAddressBytes();
                    switch (ip[0])
                    {
                        case 10:
                        case 127:
                            return true;
                        case 172:
                            return ip[1] >= 16 && ip[1] < 32;
                        case 192:
                            return ip[1] == 168;
                        default:
                            return false;
                    }
                case UriHostNameType.IPv6:
                    var ipv6 = IPAddress.Parse(uri.Host);
                    return (!ipv6.IsIPv6LinkLocal && !ipv6.IsIPv6SiteLocal);
                default:
                    return false;
            }
        }

        public ICollection<ConnectedPeer> GetPeersIn()
        {
            return GetActivePeers()
                .Where(x => !x.Outgoing)
                .Select(x => new ConnectedPeer(x.RemoteId, x.RemoteEndPoint?.ToString()))
                .ToList();
        }

        public ICollection<ConnectedPeer> GetPeersOut()
        {
            return GetActivePeers()
                .Where(x => x.Outgoing)
                .Select(x => new ConnectedPeer(x.RemoteId, x.RemoteEndPoint?.ToString()))
                .ToList();
        }

    }

    internal class Commands
    {
        public const byte Tail = 0;
        public const byte Block = 1;
        public const byte Txn = 2;
        public const byte BlockRequest = 3;
        public const byte NextBlockRequest = 4;
        public const byte BlockHeightRequest = 5;
        public const byte PeerShare = 6;
        public const byte TxnRequest = 7;
    }
}
