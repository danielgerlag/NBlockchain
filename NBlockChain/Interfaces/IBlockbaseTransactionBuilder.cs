using System.Collections.Generic;
using NBlockchain.Models;

namespace NBlockchain.Interfaces
{
    public interface IBlockbaseTransactionBuilder
    {
        TransactionEnvelope Build(KeyPair builderKeys, ICollection<TransactionEnvelope> transactions);
    }
}