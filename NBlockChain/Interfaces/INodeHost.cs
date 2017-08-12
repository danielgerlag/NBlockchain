using System.Threading.Tasks;
using NBlockChain.Models;

namespace NBlockChain.Interfaces
{
    public interface INodeHost : IBlockReceiver, ITransactionReceiver
    {
        
    }
}