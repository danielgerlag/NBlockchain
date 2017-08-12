using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NBlockChain.Interfaces;
using NBlockChain.Models;

namespace NBlockChain.Services
{
    public class InProcessPeerNetwork : IPeerNetwork, IDisposable
    {
        private static readonly IList<IPeerNetwork> Peers = new List<IPeerNetwork>();

        private readonly IBlockReceiver _blockReciever;
        private readonly ITransactionReceiver _transactionReciever;

        public Guid NodeId { get; private set; }

        public InProcessPeerNetwork(IBlockReceiver blockReciever, ITransactionReceiver transactionReciever)
        {
            _blockReciever = blockReciever;
            _transactionReciever = transactionReciever;
            NodeId = Guid.NewGuid();
            Peers.Add(this);
        }

        public Action<Guid, Block> ReceiveBlock => (peer, block) => _blockReciever.RecieveBlock(block);
        public Action<Guid, Block> ReceiveTail => (peer, block) => _blockReciever.RecieveTail(block);
        public Action<Guid, TransactionEnvelope> ReceiveTransaction => (peer, txn) => _transactionReciever.RecieveTransaction(txn);
        
        public Action<Guid, byte[]> ReceiveBlockRequest => throw new NotImplementedException();

        public async Task BroadcastBlock(Block block)
        {
            Parallel.ForEach(Peers, peer =>
            {
                peer.ReceiveTail(NodeId, block);
            });
            await Task.Yield();
        }

        public async Task BroadcastTransaction(TransactionEnvelope transaction)
        {
            Parallel.ForEach(Peers, peer =>
            {
                peer.ReceiveTransaction(NodeId, transaction);
            });
            await Task.Yield();
        }
        
        public async Task RequestNextBlock(byte[] blockId)
        {
            
        }

        public void Dispose()
        {
            Peers.Remove(this);
        }
    }
}
