using System.Threading.Tasks;
using NBlockchain.Models;

namespace NBlockchain.Interfaces
{
    public interface ITransactionKeyResolver
    {
        Task<byte[]> ResolveKey(Transaction txn);
    }
}