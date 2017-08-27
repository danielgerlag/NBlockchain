using System;
using System.Threading.Tasks;
using System.Threading;
using NBlockchain.Models;

namespace NBlockchain.Interfaces
{
    public interface IBlockBuilder
    {
        Task<Block> BuildBlock(byte[] prevBlock, uint height, KeyPair builderKeys, CancellationToken cancellationToken);
        void QueueTransaction(TransactionEnvelope transaction);
        void FlushQueue();
    }
}