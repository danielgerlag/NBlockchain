using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NBlockChain.Models;

namespace NBlockChain.Interfaces
{
    public interface IPeerNetwork
    {
        Guid NodeId { get; }

        Task BroadcastBlock(Block block);

        Task BroadcastTransaction(TransactionEnvelope transaction);

        Task RequestNextBlock(byte[] blockId);

        Action<Guid, Block> ReceiveBlock { get; }

        Action<Guid, Block> ReceiveTail { get; }

        Action<Guid, byte[]> ReceiveBlockRequest { get; }

        Action<Guid, TransactionEnvelope> ReceiveTransaction { get; }

    }
}
