using System;
using NBlockChain.Models;
using System.Threading.Tasks;

namespace NBlockChain.Interfaces
{
    public interface IBlockBuilder<T>
        where T : AbstractTransaction
    {
        Task<Block<T>> BuildBlock(DateTime endTime, byte[] prevBlock);
        Task<int> QueueTransaction(T transaction);
    }
}