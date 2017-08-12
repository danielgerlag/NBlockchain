using System.Collections.Generic;
using NBlockChain.Models;

namespace NBlockChain.Interfaces
{
    public interface IBlockbaseTransactionBuilder
    {
        TransactionEnvelope Build(KeyPair builderKeys, ICollection<TransactionEnvelope> transactions);
    }
}