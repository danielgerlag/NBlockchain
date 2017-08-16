using System.Threading.Tasks;
using NBlockChain.Models;

namespace NBlockChain.Interfaces
{
    public interface IBlockRepository
    {
        Task AddBlock(Block block);
        Task<bool> HaveBlock(byte[] blockId);
        Task<bool> IsEmpty();
        Task<BlockHeader> GetNewestBlockHeader();
        Task<Block> GetNextBlock(byte[] prevBlockId);
        Task<long> GetGenesisBlockTime();
    }
}