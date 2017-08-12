using System.Threading.Tasks;
using NBlockChain.Models;
using System.Threading;

namespace NBlockChain.Interfaces
{
    public interface IBlockNotarizer
    {
        Task Notarize(Block block, CancellationToken cancellationToken);
    }
}