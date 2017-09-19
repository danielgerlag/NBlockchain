using System.Threading.Tasks;
using NBlockchain.Models;

namespace NBlockchain.Interfaces
{
    public interface IBlockchainHost
    {
        Task SendTransaction(Transaction transaction);
    }
}