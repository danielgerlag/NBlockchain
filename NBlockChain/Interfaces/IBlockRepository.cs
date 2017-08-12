using System.Threading.Tasks;
using NBlockChain.Models;

namespace NBlockChain.Interfaces
{
    public interface IBlockRepository
    {
        Task AddBlock(Block block);
        Task<bool> HaveBlock(byte[] blockId);

        Task<BlockHeader> GetNewestBlockHeader();

        Task<long> GetGenesisBlockTime();
    }
}