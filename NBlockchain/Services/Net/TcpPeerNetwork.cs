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

namespace NBlockchain.Services.Net
{
    public class TcpPeerNetwork : IPeerNetwork, IDisposable
    {
        //TODO: break this class up into smaller pieces
        private const int TargetOutgoingCount = 8;
        private readonly uint _port;
        private byte[] _serviceId = new byte[] { 0x0, 0x1 };

        private IBlockReceiver _blockReciever;
        private ITransactionReceiver _transactionReciever;

        private readonly IBlockRepository _blockRepository;
        private readonly IEnumerable<IPeerDiscoveryService> _discoveryServices;
        private readonly ILogger _logger;
        private readonly IOwnAddressResolver _ownAddressResolver;

        private readonly ConcurrentQueue<KnownPeer> _peerRoundRobin = new ConcurrentQueue<KnownPeer>();
        
        private readonly List<PeerConnection> _peerConnections = new List<PeerConnection>();
        private CancellationTokenSource _cancelTokenSource;
        private TcpListener _listener;

        private readonly AutoResetEvent _peerEvent = new AutoResetEvent(true);

        private Timer _sharePeersTimer;
        private Timer _discoveryTimer;

        private string _internalConnsctionString;
        private string _externalConnsctionString;

        public Guid NodeId { get; private set; }

        public TcpPeerNetwork(uint port, IBlockRepository blockRepository, IEnumerable<IPeerDiscoveryService> discoveryServices, ILoggerFactory loggerFactory, IOwnAddressResolver ownAddressResolver)
        {
            _port = port;
            _logger = loggerFactory.CreateLogger<TcpPeerNetwork>();
            _blockRepository = blockRepository;
            _discoveryServices = discoveryServices;
            _ownAddressResolver = ownAddressResolver;
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
            _listener = new TcpListener((int)_port);
            _listener.Start();

            Task.Factory.StartNew(() =>
            {
                while (!_cancelTokenSource.IsCancellationRequested)
                {
                    var client = _listener.AcceptTcpClient();
                    _logger.LogDebug($"Client connected - {client.Client.RemoteEndPoint}");
                    var peer = new PeerConnection(_serviceId, NodeId, client);
                    peer.OnReceiveMessage += Peer_OnReceiveMessage;
                    peer.OnDisconnect += Peer_OnDisconnect;
                    _peerEvent.WaitOne();
                    try
                    {
                        _peerConnections.Add(peer);
                    }
                    finally
                    {
                        _peerEvent.Set();
                    }
                }
            });


            DiscoverOwnConnectionStrings();

            _discoveryTimer = new Timer((state) => 
            {
                DiscoverPeers();
                AdvertiseToPeers();
            }, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30));

            _sharePeersTimer = new Timer(SharePeers, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        private async void Peer_OnReceiveMessage(PeerConnection sender, byte command, byte[] data)
        {
            switch (command)
            {
                case Commands.Block:
                    await ProcessBlock(data, sender.RemoteId, false);
                    break;
                case Commands.BlockRequest:
                    await ProcessBlockRequest(data, sender);
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
            }
        }

        private async Task ProcessBlockRequest(byte[] prevBlockId, PeerConnection peer)
        {
            var block = await _blockRepository.GetNextBlock(prevBlockId);
            if (block != null)
            {
                _logger.LogDebug("Responding to block request");
                var data = SerializeObject(block);                
                SendBlock(peer, data, false);
            }
            else
            {
                _logger.LogDebug("Unable to respond to block request");
            }
        }

        private async Task ProcessBlock(byte[] data, Guid originId, bool tail)
        {
            _logger.LogDebug($"Processing block {tail}");
            var block = DeserializeObject<Block>(data);
            var result = PeerDataResult.Ignore;
            if (tail)
                result = await _blockReciever.RecieveTail(block);
            else
                result = await _blockReciever.RecieveBlock(block);

            if ((tail) && (result == PeerDataResult.Relay))
            {
                var relayTask = Task.Factory.StartNew(() =>
                {
                    var peerList = GetActivePeers().Where(x => x.RemoteId != originId);
                    Parallel.ForEach(peerList, peer =>
                    {
                        SendBlock(peer, data, tail);
                    });
                });
            }
        }

        private async Task ProcessTransaction(byte[] data, Guid originId)
        {
            var txn = DeserializeObject<TransactionEnvelope>(data);
            var result = await _transactionReciever.RecieveTransaction(txn);

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
        }
                

        private void Peer_OnDisconnect(PeerConnection sender)
        {
            _peerEvent.WaitOne();
            try
            {
                _peerConnections.Remove(sender);
            }
            finally
            {
                _peerEvent.Set();
            }
        }
        
        private void OnboardPeer(string connStr)
        {
            try
            {
                var peer = new PeerConnection(_serviceId, NodeId);                
                peer.Connect(connStr);
                peer.OnReceiveMessage += Peer_OnReceiveMessage;
                peer.OnDisconnect += Peer_OnDisconnect;
                _peerEvent.WaitOne();
                try
                {
                    _peerConnections.Add(peer);
                }
                finally
                {
                    _peerEvent.Set();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error connecting to {connStr} - {ex.Message}");
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

        public void DiscoverPeers()
        {
            foreach (var discovery in _discoveryServices)
            {
                Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        var newPeers = await discovery.DiscoverPeers();
                        foreach (var np in newPeers)
                            AddPeer(np);
                        ConnectOut();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                    }
                });
            }
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

            //
        }
                
        private void ConnectOut()
        {
            var peersOut = GetActivePeers().Where(x => x.Outgoing);
            var target = (TargetOutgoingCount - peersOut.Count());
            if (target <= 0)
                return;

            var actual = 0;
            var counter = 0;
            lock (_peerRoundRobin)
            {
                while ((actual < target) && (counter < _peerRoundRobin.Count))
                {
                    if (_peerRoundRobin.TryDequeue(out var kp))
                    {
                        _peerRoundRobin.Enqueue(kp);
                        counter++;

                        if (peersOut.Any(x => x.ConnectionString == kp.ConnectionString))
                            continue;

                        _logger.LogDebug($"Connecting to {kp.ConnectionString}");
                        OnboardPeer(kp.ConnectionString);
                        actual++;
                    }
                }
            }
        }

        public void RegisterBlockReceiver(IBlockReceiver blockReceiver)
        {
            _blockReciever = blockReceiver;
        }

        public void RegisterTransactionReceiver(ITransactionReceiver transactionReciever)
        {
            _transactionReciever = transactionReciever;
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

        public void BroadcastTransaction(TransactionEnvelope transaction)
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
            Task.Factory.StartNew(async () =>
            {
                var peers = GetActivePeers().Where(x => x.RemoteId != NodeId);
                foreach (var peer in peers)
                {
                    _logger.LogDebug($"Requesting block from incoming peer {peer.RemoteId}");
                    peer.Send(Commands.BlockRequest, blockId);

                    await Task.Delay(TimeSpan.FromSeconds(5));

                    if ((await _blockRepository.GetNextBlock(blockId)) != null)
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
                        if (_internalConnsctionString != null)
                            ds.AdvertiseLocal(_internalConnsctionString);

                        if (_externalConnsctionString != null)
                            ds.AdvertiseGlobal(_externalConnsctionString);
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
                _internalConnsctionString = $"tcp://{ownAddress}:{_port}";

            //TODO: external addresses
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
                .Select(x => new ConnectedPeer(x.RemoteId, x.RemoteEndPoint.ToString()) { LastContact = x.LastContact.Value })
                .ToList();
        }

        public ICollection<ConnectedPeer> GetPeersOut()
        {
            return GetActivePeers()
                .Where(x => x.Outgoing)
                .Select(x => new ConnectedPeer(x.RemoteId, x.RemoteEndPoint.ToString()) { LastContact = x.LastContact.Value })
                .ToList();
        }
        
    }

    internal class Commands
    {
        public const byte Tail = 0;
        public const byte Block = 1;
        public const byte Txn = 2;
        public const byte BlockRequest = 3;
        public const byte PeerShare = 4;
    }   
}
