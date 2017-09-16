using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NBlockchain.Interfaces;
using NBlockchain.Models;

namespace NBlockchain.Services.Net
{
    public class InProcessPeerNetwork : IPeerNetwork, IDisposable
    {
        private static readonly IList<InProcessPeerNetwork> Peers = new List<InProcessPeerNetwork>();
        private readonly IBlockRepository _blockRepository;

        private IBlockReceiver _blockReciever;
        private ITransactionReceiver _transactionReciever;

        public Guid NodeId { get; private set; }

        public InProcessPeerNetwork(IBlockRepository blockRepository)
        {
            _blockRepository = blockRepository;
            NodeId = Guid.NewGuid();
            Peers.Add(this);
        }

        public void RegisterBlockReceiver(IBlockReceiver blockReceiver)
        {
            _blockReciever = blockReceiver;
        }

        public void RegisterTransactionReceiver(ITransactionReceiver transactionReciever)
        {
            _transactionReciever = transactionReciever;
        }

#pragma warning disable CS1998
        public async Task DiscoverPeers()
        {
        }
#pragma warning restore CS1998

        public void Open()
        {
        }

        public void Close()
        {
        }

        public Action<Guid, Block> ReceiveBlock => (peer, block) =>
        {
            _blockReciever.RecieveBlock(block, false);
        };


        public Action<Guid, Block> ReceiveTail => (peer, block) =>
        {
            _blockReciever.RecieveBlock(block, true);
        };

        public Action<Guid, Transaction> ReceiveTransaction => (peer, txn) =>
        {
            _transactionReciever.RecieveTransaction(txn);
        };

        public Action<Guid, byte[]> ReceiveBlockRequest => async (peer, txn) =>
        {
            var block = await _blockRepository.GetNextBlock(txn);
            if (block == null)
                return;

            var dest = Peers.First(x => x.NodeId == peer);
            var task = Task.Factory.StartNew(() => dest.ReceiveBlock(NodeId, block));
        };

        public void BroadcastTail(Block block)
        {
            Parallel.ForEach(Peers.Where(x => x.NodeId != NodeId), peer =>
            {
                var task = Task.Factory.StartNew(() => peer.ReceiveTail(NodeId, block));
            });
        }
        
        public void BroadcastTransaction(Transaction transaction)
        {
            Parallel.ForEach(Peers.Where(x => x.NodeId != NodeId), peer =>
            {
                var task = Task.Factory.StartNew(() => peer.ReceiveTransaction(NodeId, transaction));
            });
        }

        public void RequestNextBlock(byte[] blockId)
        {
            Parallel.ForEach(Peers.Where(x => x.NodeId != NodeId).Take(2), peer =>
            {
                var task = Task.Factory.StartNew(() => peer.ReceiveBlockRequest(NodeId, blockId));
            });
        }

        public void Dispose()
        {
            Peers.Remove(this);
        }

        public ICollection<ConnectedPeer> GetPeersIn()
        {
            throw new NotImplementedException();
        }

        public ICollection<ConnectedPeer> GetPeersOut()
        {
            throw new NotImplementedException();
        }

        public void RequestBlock(byte[] blockId)
        {
            throw new NotImplementedException();
        }
    }
}
