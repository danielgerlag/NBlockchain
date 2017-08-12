using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NBlockChain.Models;

namespace NBlockChain.Interfaces
{
    public interface IPeerNetwork
    {

        Task BroadcastBlock(Block block);

        Task BroadcastTransaction(TransactionEnvelope transaction);

        Task RequestNextBlock(byte[] blockId);

        Action<Block> ReceiveBlock { get; }

        Action<Block> ReceiveTail { get; }

        Action<TransactionEnvelope> ReceiveTransaction { get; }

    }
}
