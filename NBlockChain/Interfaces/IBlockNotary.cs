using System.Threading.Tasks;
using NBlockChain.Models;
using System.Threading;

namespace NBlockChain.Interfaces
{
    public interface IBlockNotary
    {
        Task ConfirmBlock(Block block, CancellationToken cancellationToken);
    }
}