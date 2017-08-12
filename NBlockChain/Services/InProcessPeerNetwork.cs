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

        public InProcessPeerNetwork(IBlockReceiver blockReciever, ITransactionReceiver transactionReciever)
        {
            _blockReciever = blockReciever;
            _transactionReciever = transactionReciever;
            Peers.Add(this);
        }

        public Action<Block> ReceiveBlock => (block) => _blockReciever.RecieveBlock(block);
        public Action<TransactionEnvelope> ReceiveTransaction => (txn) => _transactionReciever.RecieveTransaction(txn);

        public async Task BroadcastBlock(Block block)
        {
            Parallel.ForEach(Peers, peer =>
            {
                peer.ReceiveBlock(block);
            });
            await Task.Yield();
        }

        public async Task BroadcastTransaction(TransactionEnvelope transaction)
        {
            Parallel.ForEach(Peers, peer =>
            {
                peer.ReceiveTransaction(transaction);
            });
            await Task.Yield();
        }

        public void Dispose()
        {
            Peers.Remove(this);
        }
    }
}
