using System.Threading.Tasks;
using NBlockchain.Models;

namespace NBlockchain.Interfaces
{
    public interface INodeHost
    {
        Task SendTransaction(Transaction transaction);
    }
}