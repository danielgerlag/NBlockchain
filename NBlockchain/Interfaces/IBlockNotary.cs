using System.Threading.Tasks;
using System.Threading;
using NBlockchain.Models;

namespace NBlockchain.Interfaces
{
    public interface IBlockNotary
    {
        Task ConfirmBlock(Block block, CancellationToken cancellationToken);
    }
}