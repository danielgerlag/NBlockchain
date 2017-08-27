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

    }
}
