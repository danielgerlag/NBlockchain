using System.Threading.Tasks;
using NBlockchain.Models;

namespace NBlockchain.Interfaces
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