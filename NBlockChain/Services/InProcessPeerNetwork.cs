using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NBlockChain.Interfaces;
using NBlockChain.Models;

namespace NBlockChain.Services
{
    public class InProcessPeerNetwork : IPeerNetwork, IDisposable
    {
        private static readonly IList<InProcessPeerNetwork> Peers = new List<InProcessPeerNetwork>();
        private readonly ICollection<IBlockReceiver> _blockRecievers = new HashSet<IBlockReceiver>();
        private readonly ICollection<ITransactionReceiver> _transactionRecievers = new HashSet<ITransactionReceiver>();
        private readonly IBlockRepository _blockRepository;

        public Guid NodeId { get; private set; }

        public InProcessPeerNetwork(IBlockRepository blockRepository)
        {
            _blockRepository = blockRepository;
            NodeId = Guid.NewGuid();
            Peers.Add(this);
        }

        public void RegisterBlockReceiver(IBlockReceiver blockReceiver)
        {
            _blockRecievers.Add(blockReceiver);
        }

        public void RegisterTransactionReceiver(ITransactionReceiver transactionReciever)
        {
            _transactionRecievers.Add(transactionReciever);
        }

        public void DeregisterBlockReceiver(IBlockReceiver blockReceiver)
        {
            _blockRecievers.Remove(blockReceiver);
        }

        public void DeregisterTransactionReceiver(ITransactionReceiver transactionReciever)
        {
            _transactionRecievers.Remove(transactionReciever);
        }

        public Action<Guid, Block> ReceiveBlock => (peer, block) =>
        {
            foreach (var recv in _blockRecievers)
                recv.RecieveBlock(block);
        };


        public Action<Guid, Block> ReceiveTail => (peer, block) =>
        {
            foreach (var recv in _blockRecievers)
                recv.RecieveTail(block);
        };

        public Action<Guid, TransactionEnvelope> ReceiveTransaction => (peer, txn) =>
        {
            foreach (var recv in _transactionRecievers)
                recv.RecieveTransaction(txn);
        };

        public Action<Guid, byte[]> ReceiveBlockRequest => async (peer, txn) =>
        {
            var block = await _blockRepository.GetNextBlock(txn);
            if (block == null)
                return;

            var dest = Peers.First(x => x.NodeId == peer);
            var task = Task.Factory.StartNew(() => dest.ReceiveBlock(NodeId, block));
        };

        public async Task BroadcastBlock(Block block)
        {
            Parallel.ForEach(Peers.Where(x => x.NodeId != NodeId), peer =>
            {
                var task = Task.Factory.StartNew(() => peer.ReceiveTail(NodeId, block));
            });
            await Task.Yield();
        }

        public async Task BroadcastTransaction(TransactionEnvelope transaction)
        {
            Parallel.ForEach(Peers.Where(x => x.NodeId != NodeId), peer =>
            {
                var task = Task.Factory.StartNew(() => peer.ReceiveTransaction(NodeId, transaction));
            });
            await Task.Yield();
        }

        public async Task RequestNextBlock(byte[] blockId)
        {
            Parallel.ForEach(Peers.Where(x => x.NodeId != NodeId).Take(2), peer =>
            {
                var task = Task.Factory.StartNew(() => peer.ReceiveBlockRequest(NodeId, blockId));
            });
            await Task.Yield();
        }

        public void Dispose()
        {
            Peers.Remove(this);
        }
    }
}
