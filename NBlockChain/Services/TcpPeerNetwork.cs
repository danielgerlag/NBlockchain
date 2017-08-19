using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBlockChain.Interfaces;
using NBlockChain.Models;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace NBlockChain.Services
{
    public class TcpPeerNetwork : IPeerNetwork, IDisposable
    {
        private const int TargetOutgoingCount = 8;
        private readonly uint _port;

        private IBlockReceiver _blockReciever;
        private ITransactionReceiver _transactionReciever;

        private readonly IBlockRepository _blockRepository;
        private readonly IEnumerable<IPeerDiscoveryService> _discoveryServices;
        private readonly ILogger _logger;

        private readonly ConcurrentDictionary<Guid, PeerNode> _knownPeers = new ConcurrentDictionary<Guid, PeerNode>();

        private readonly ConcurrentDictionary<Guid, DateTime> _incomingPeerLastContact = new ConcurrentDictionary<Guid, DateTime>();

        private Timer _sharePeersTimer;

        private readonly RouterSocket _incomingSocket = new RouterSocket();
        private readonly NetMQPoller _poller = new NetMQPoller();
        private NetMQTimer _houseKeeper;

        private readonly ConcurrentDictionary<Guid, NetMQSocket> _outgoingSockets = new ConcurrentDictionary<Guid, NetMQSocket>();

        public Guid NodeId { get; private set; }

        public TcpPeerNetwork(uint port, IBlockRepository blockRepository, IEnumerable<IPeerDiscoveryService> discoveryServices, ILoggerFactory loggerFactory)
        {
            _port = port;
            _logger = loggerFactory.CreateLogger<TcpPeerNetwork>();
            _blockRepository = blockRepository;
            _discoveryServices = discoveryServices;
            NodeId = Guid.NewGuid();
            _sharePeersTimer = new Timer(SharePeers, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            _incomingSocket.ReceiveReady += IncomingSocketReceiveReady;
        }

        private async void IncomingSocketReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            try
            {
                var message = e.Socket.ReceiveMultipartMessage();

                if (message.FrameCount > 1)
                {
                    var clientId = new Guid(message[0].Buffer);
                    var op = (MessageOp) (message[1].Buffer.First());
                    _incomingPeerLastContact[clientId] = DateTime.Now;

                    switch (op)
                    {
                        case MessageOp.Block:
                            await ProcessBlock(message, clientId, false);
                            break;
                        case MessageOp.Tail:
                            await ProcessBlock(message, clientId, true);
                            break;
                        case MessageOp.Txn:
                            await ProcessTransaction(message, clientId);
                            break;
                        case MessageOp.BlockRequest:
                            await ProcessBlockRequest(message, e.Socket, clientId);
                            break;

                        case MessageOp.Connect:
                            _logger.LogDebug("Recv connect from {0}", clientId);
                            _incomingSocket.SendMoreFrame(message[0].Buffer)
                                .SendMoreFrame(NodeId.ToByteArray())
                                .SendFrame(ConvertOp(MessageOp.Identify));
                            break;

                        case MessageOp.Disconnect:
                            _logger.LogDebug("Recv disconnect from {0}", clientId);
                            _incomingPeerLastContact.TryRemove(clientId, out var v);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing serv message, {ex.Message}");
            }
        }

        private async void Peer_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            try
            {
                var message = e.Socket.ReceiveMultipartMessage();
                if (message.FrameCount > 1)
                {
                    var serverId = new Guid(message[0].Buffer);
                    var op = (MessageOp) (message[1].Buffer.First());

                    switch (op)
                    {
                        case MessageOp.Block:
                            await ProcessBlock(message, serverId, false);
                            break;
                        case MessageOp.Tail:
                            await ProcessBlock(message, serverId, true);
                            break;
                        case MessageOp.Txn:
                            await ProcessTransaction(message, serverId);
                            break;
                        case MessageOp.BlockRequest:
                            await ProcessBlockRequest(message, e.Socket, null);
                            break;

                        case MessageOp.Identify:
                            _logger.LogDebug("Recv identify from {0}", serverId);
                            _outgoingSockets[serverId] = e.Socket;
                            break;

                        case MessageOp.Disconnect:
                            _logger.LogDebug("Recv disconnect from {0}", serverId);
                            e.Socket.Close();
                            _outgoingSockets.TryRemove(serverId, out var sock);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing peer message, {ex.Message}");
            }
        }

        private async Task ProcessBlockRequest(NetMQMessage message, IOutgoingSocket socket, Guid? peerId)
        {
            var prevBlockId = message[2].Buffer;
            var block = await _blockRepository.GetNextBlock(prevBlockId);
            if (block != null)
            {
                _logger.LogDebug("Responding to block request");
                var data = SerializeObject(block);
                SendBlock(socket, false, peerId, data, -1);
            }
            else
            {
                _logger.LogDebug("Unable to respond to block request");
            }
        }

        private async Task ProcessBlock(NetMQMessage message, Guid originId, bool tail)
        {
            _logger.LogDebug($"Processing block {tail}");
            var hopCount = message[2].ConvertToInt32();
            var block = DeserializeObject<Block>(message[3].Buffer);
            var result = false;
            if (tail)
                result = await _blockReciever.RecieveTail(block);
            else
                result = await _blockReciever.RecieveBlock(block);

            if ((tail) && (result) && (hopCount > -1))
            {
                var incomingTask = Task.Factory.StartNew(() =>
                {
                    var peerList = GetIncomingPeers().Where(x => x != originId);
                    Parallel.ForEach(peerList, peerId =>
                    {
                        SendBlock(_incomingSocket, tail, peerId, message[3].Buffer, hopCount + 1);
                    });
                });

                var outgoingTask = Task.Factory.StartNew(() =>
                {
                    var peerList = GetOutgoingPeers().Where(x => x != originId);
                    Parallel.ForEach(peerList, peerId =>
                    {
                        SendBlock(_outgoingSockets[peerId], tail, null, message[3].Buffer, hopCount + 1);
                    });
                });
            }
        }

        private async Task ProcessTransaction(NetMQMessage message, Guid originId)
        {
            var hopCount = message[2].ConvertToInt32();
            var txn = DeserializeObject<TransactionEnvelope>(message[3].Buffer);
            var result = await _transactionReciever.RecieveTransaction(txn);

            if ((result) && (hopCount > -1))
            {
                var incomingTask = Task.Factory.StartNew(() =>
                {
                    var peerList = GetIncomingPeers().Where(x => x != originId);
                    Parallel.ForEach(peerList, peerId =>
                    {
                        SendTxn(_incomingSocket, peerId, message[3].Buffer, hopCount + 1);
                    });
                });

                var outgoingTask = Task.Factory.StartNew(() =>
                {
                    var peerList = GetOutgoingPeers().Where(x => x != originId);
                    Parallel.ForEach(peerList, peerId =>
                    {
                        SendTxn(_outgoingSockets[peerId], null, message[3].Buffer, hopCount + 1);
                    });
                });
            }
        }

        public void Open()
        {
            _incomingSocket.Bind($"tcp://*:{_port}");
            _poller.Add(_incomingSocket);
            _poller.RunAsync();
            _houseKeeper = new NetMQTimer(TimeSpan.FromSeconds(30));
            _houseKeeper.Elapsed += HouseKeeper_Elapsed;
            _poller.Add(_houseKeeper);
            _houseKeeper.Enable = true;
            DiscoverPeers();
        }

        private void OnboardPeer(string connStr)
        {
            var peer = new DealerSocket();
            peer.Options.Identity = NodeId.ToByteArray();
            peer.ReceiveReady += Peer_ReceiveReady;
            peer.Connect(connStr);
            _poller.Add(peer);
            //_outgoingSockets[]
            peer.SendFrame(ConvertOp(MessageOp.Connect));
        }

        public void Close()
        {
            foreach (var peerId in GetIncomingPeers())
            {
                _incomingSocket
                    .SendMoreFrame(peerId.ToByteArray())
                    .SendMoreFrame(NodeId.ToByteArray())
                    .SendFrame(ConvertOp(MessageOp.Disconnect));
            }

            foreach (var peerId in GetOutgoingPeers())
            {
                _outgoingSockets[peerId]
                    .SendFrame(ConvertOp(MessageOp.Disconnect));

                _outgoingSockets[peerId].Close();
            }

            _outgoingSockets.Clear();

            _poller.Stop();
            _poller.Remove(_incomingSocket);
            _poller.Remove(_houseKeeper);
            _houseKeeper.Enable = false;
            _incomingSocket.Close();
        }

        public void DiscoverPeers()
        {
            foreach (var discovery in _discoveryServices)
            {
                Task.Factory.StartNew(async () =>
                {
                    var newPeers = await discovery.DiscoverPeers();
                    foreach (var np in newPeers)
                        _knownPeers[np.NodeId] = np;

                    ConnectOut();
                });
            }
        }

        private async void SharePeers(object state)
        {
            foreach (var ds in _discoveryServices)
                await ds.SharePeers(_knownPeers.Values);

            //
        }

        private void HouseKeeper_Elapsed(object sender, NetMQTimerEventArgs e)
        {
            _logger.LogDebug("Performing house keeping");
            ConnectOut();
        }

        private void ConnectOut()
        {
            var target = (TargetOutgoingCount - _outgoingSockets.Count);
            if (target <= 0)
                return;

            var actual = 0;
            foreach (var kp in _knownPeers)
            {
                if (_outgoingSockets.ContainsKey(kp.Key))
                    continue;

                _logger.LogDebug($"Connecting to {kp.Value.ConnectionString}");
                OnboardPeer(kp.Value.ConnectionString);
                actual++;
                if (actual >= target)
                    return;
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
            var incoming = GetIncomingPeers();
            var outgoing = GetOutgoingPeers();
            
            Task.Factory.StartNew(() =>
            {
                Parallel.ForEach(incoming, peerId =>
                {
                    SendBlock(_incomingSocket, true, peerId, data, 0);
                });
            });

            Task.Factory.StartNew(() =>
            {
                Parallel.ForEach(outgoing, peerId =>
                {
                    SendBlock(_outgoingSockets[peerId], true, null, data, 0);
                });
            });
        }

        private void SendBlock(IOutgoingSocket socket, bool tail, Guid? peerId, byte[] data, int hopCount)
        {
            try
            {
                var op = ConvertOp(MessageOp.Block);
                if (tail)
                    op = ConvertOp(MessageOp.Tail);

                var msg = new NetMQMessage();

                if ((peerId.HasValue))
                {
                    msg.Append(peerId.Value.ToByteArray());
                    msg.Append(NodeId.ToByteArray());
                }

                msg.Append(op);
                msg.Append(BitConverter.GetBytes(hopCount));
                msg.Append(data);

                socket.SendMultipartMessage(msg);
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
                var incoming = GetIncomingPeers();
                Parallel.ForEach(incoming, peerId =>
                {
                    SendTxn(_incomingSocket, peerId, data, 0);
                });
            });

            Task.Factory.StartNew(() =>
            {
                var outgoing = GetOutgoingPeers();
                Parallel.ForEach(outgoing, peerId =>
                {
                    SendTxn(_outgoingSockets[peerId], null, data, 0);
                });
            });
        }

        private void SendTxn(IOutgoingSocket socket, Guid? peerId, byte[] data, int hopCount)
        {
            try
            {
                var op = ConvertOp(MessageOp.Txn);

                var msg = new NetMQMessage();

                if ((peerId.HasValue) && (socket is RouterSocket))
                {
                    msg.Append(peerId.Value.ToByteArray());
                    msg.Append(NodeId.ToByteArray());
                }

                msg.Append(op);
                msg.Append(BitConverter.GetBytes(hopCount));
                msg.Append(data);

                socket.SendMultipartMessage(msg);
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
                var incoming = GetIncomingPeers();
                foreach (var peerId in incoming)
                {
                    _logger.LogDebug($"Requesting block from incoming peer {peerId}");
                    _incomingSocket
                        .SendMoreFrame(peerId.ToByteArray())
                        .SendMoreFrame(NodeId.ToByteArray())
                        .SendMoreFrame(ConvertOp(MessageOp.BlockRequest))
                        .SendFrame(blockId);

                    await Task.Delay(TimeSpan.FromSeconds(30));

                    if ((await _blockRepository.GetNextBlock(blockId)) != null)
                        return;
                }

                var outgoing = GetOutgoingPeers();
                foreach (var peerId in outgoing)
                {
                    _logger.LogDebug($"Requesting block from outgoing peer {peerId}");
                    _outgoingSockets[peerId]
                        .SendMoreFrame(ConvertOp(MessageOp.BlockRequest))
                        .SendFrame(blockId);

                    await Task.Delay(TimeSpan.FromSeconds(30));

                    if ((await _blockRepository.GetNextBlock(blockId)) != null)
                        return;
                }
            });
        }

        public void Dispose()
        {
            
        }

        private ICollection<Guid> GetIncomingPeers()
        {
            return _incomingPeerLastContact.Select(x => x.Key).ToList();
        }

        private ICollection<Guid> GetOutgoingPeers()
        {
            return _outgoingSockets.Keys;
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
                return bw.GetBuffer();
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

        private byte[] ConvertOp(MessageOp op)
        {
            byte[] result = new byte[1];
            result[0] = (byte)op;
            return result;
        }

        enum MessageOp { Disconnect = 0, Tail = 1, Block = 2, Txn = 3, BlockRequest = 4, PeerShare = 5, Connect = 6, Identify = 7 }
        
    }
}
