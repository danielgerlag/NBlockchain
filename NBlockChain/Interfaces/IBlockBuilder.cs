using System;
using NBlockChain.Models;
using System.Threading.Tasks;

namespace NBlockChain.Interfaces
{
    public interface IBlockBuilder
    {
        Task<Block> BuildBlock(byte[] prevBlock);
        Task<int> QueueTransaction(TransactionEnvelope transaction);
    }
}