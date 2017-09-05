using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NBlockchain.Models;

namespace NBlockchain.Interfaces
{
    public interface IPeerNetwork
    {
        Guid NodeId { get; }

        void BroadcastTail(Block block);

        void BroadcastTransaction(TransactionEnvelope transaction);

        void RequestNextBlock(byte[] blockId);
        
        void RegisterBlockReceiver(IBlockReceiver blockReceiver);

        void RegisterTransactionReceiver(ITransactionReceiver transactionReciever);

        void DiscoverPeers();

        void Open();

        void Close();

        ICollection<ConnectedPeer> GetPeersIn();
        ICollection<ConnectedPeer> GetPeersOut();

    }

    public class ConnectedPeer
    {
        public Guid NodeId { get; set; }
        public string Address { get; set; }
        public DateTime LastContact { get; set; }

        public ConnectedPeer(Guid nodeId, string address)
        {
            NodeId = nodeId;
            Address = address;
            LastContact = DateTime.Now;
        }
    }
}
