using System.Threading.Tasks;
using NBlockchain.Models;

namespace NBlockchain.Interfaces
{
    public interface INodeHost : IBlockReceiver, ITransactionReceiver
    {
        Task SendTransaction(Transaction transaction);
    }
}