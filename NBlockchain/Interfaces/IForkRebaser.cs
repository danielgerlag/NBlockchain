using System.Threading.Tasks;
using NBlockchain.Models;

namespace NBlockchain.Interfaces
{
    public interface IForkRebaser
    {
        Task<BlockHeader> FindKnownForkbase(byte[] forkTipId);
        Task RebaseChain(byte[] divergentId, byte[] targetTipId);
        void RegisterBlockReceiver(IBlockReceiver blockReceiver);
    }
}