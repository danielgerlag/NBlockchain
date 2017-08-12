using NBlockChain.Models;

namespace NBlockChain.Interfaces
{
    public interface IBlockVerifier
    {
        bool Verify(Block block);
    }
}