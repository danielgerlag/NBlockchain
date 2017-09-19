using System.Collections.Generic;
using System.Threading.Tasks;
using NBlockchain.Models;

namespace NBlockchain.Interfaces
{
    public interface IForkRebaser
    {
        Task<BlockHeader> FindKnownForkbase(byte[] forkTipId);
        Task<ICollection<Block>> RebaseChain(byte[] divergentId, byte[] targetTipId);
    }
}