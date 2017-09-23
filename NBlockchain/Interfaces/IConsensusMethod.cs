using System.Threading.Tasks;
using System.Threading;
using NBlockchain.Models;

namespace NBlockchain.Interfaces
{
    public interface IConsensusMethod
    {
        Task BuildConsensus(Block block, CancellationToken cancellationToken);
        bool VerifyConsensus(Block block);
    }
}