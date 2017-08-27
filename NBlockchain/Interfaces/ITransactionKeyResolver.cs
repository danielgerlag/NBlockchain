using NBlockchain.Models;

namespace NBlockchain.Interfaces
{
    public interface ITransactionKeyResolver
    {
        byte[] ResolveKey(TransactionEnvelope txn);
    }
}