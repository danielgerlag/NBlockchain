using System;
using System.Threading.Tasks;
using NBlockchain.Models;
using System.Collections.Generic;

namespace NBlockchain.Interfaces
{
    public interface IBlockRepository
    {
        Task AddBlock(Block block);
        Task<bool> HaveBlock(byte[] blockId);
        Task<bool> IsEmpty();
        Task<BlockHeader> GetNewestBlockHeader();
        Task<Block> GetNextBlock(byte[] prevBlockId);

        Task<BlockHeader> GetBlockHeader(byte[] blockId);
        Task<Block> GetBlock(byte[] blockId);

        Task<BlockHeader> GetMainChainHeader(uint height);
        Task<BlockHeader> GetForkHeader(byte[] forkBlockId);

        Task AddDetachedBlock(Block block);

        Task<BlockHeader> GetDivergentHeader(byte[] forkTipBlockId);
                
        Task RewindChain(byte[] blockId);

        Task<ICollection<Block>> GetFork(byte[] forkTipBlockId);
                

        Task<int> GetAverageBlockTimeInSecs(DateTime startUtc, DateTime endUtc);
        
    }
}