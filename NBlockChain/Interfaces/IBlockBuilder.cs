using System;
using NBlockChain.Models;
using System.Threading.Tasks;

namespace NBlockChain.Interfaces
{
    public interface IBlockBuilder
    {
        Task<Block> BuildBlock(byte[] prevBlock, KeyPair builderKeys);
        Task<int> QueueTransaction(TransactionEnvelope transaction);
        void FlushQueue();
    }
}