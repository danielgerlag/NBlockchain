using System;
using System.Threading.Tasks;
using NBlockchain.Models;
using System.Collections.Generic;

namespace NBlockchain.Interfaces
{
    public interface IBlockRepository
    {
        Task AddBlock(Block block);
        Task<bool> HavePrimaryBlock(byte[] blockId);
        Task<bool> HaveSecondaryBlock(byte[] blockId);
        Task<bool> IsEmpty();
        Task<BlockHeader> GetBestBlockHeader();
        Task<Block> GetNextBlock(byte[] prevBlockId);

        Task<BlockHeader> GetBlockHeader(byte[] blockId);
        Task<Block> GetBlock(byte[] blockId);

        Task<BlockHeader> GetPrimaryHeader(uint height);
        Task<BlockHeader> GetSecondaryHeader(byte[] forkBlockId);

        Task AddSecondaryBlock(Block block);

        Task<BlockHeader> GetDivergentHeader(byte[] forkTipBlockId);
                
        Task RewindChain(byte[] blockId);

        Task<ICollection<Block>> GetFork(byte[] forkTipBlockId);

        Task DiscardSecondaryBlock(byte[] blockId);

        Task<int> GetAverageBlockTimeInSecs(DateTime startUtc, DateTime endUtc);
        
    }
}