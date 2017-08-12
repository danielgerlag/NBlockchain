using System.Threading.Tasks;
using NBlockChain.Models;

namespace NBlockChain.Interfaces
{
    public interface IBlockRepository
    {
        Task AddBlock(Block block);
        Task<bool> HaveBlock(Block block);

        Task<BlockHeader> GetNewestBlockHeader();
    }
}