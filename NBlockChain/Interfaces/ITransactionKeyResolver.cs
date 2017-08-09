using NBlockChain.Models;

namespace NBlockChain.Interfaces
{
    public interface ITransactionKeyResolver
    {
        byte[] ResolveKey(TransactionEnvelope txn);
    }
}