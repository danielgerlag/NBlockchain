using System;
using NBlockChain.Models;
using System.Threading.Tasks;
using System.Threading;

namespace NBlockChain.Interfaces
{
    public interface IBlockBuilder
    {
        Task<Block> BuildBlock(byte[] prevBlock, uint height, KeyPair builderKeys, CancellationToken cancellationToken);
        Task QueueTransaction(TransactionEnvelope transaction);
        void FlushQueue();
    }
}