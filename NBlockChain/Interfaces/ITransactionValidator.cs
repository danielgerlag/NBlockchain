using NBlockChain.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NBlockChain.Interfaces
{
    public interface ITransactionValidator
    {
        string TransactionType { get; }
        int Validate(TransactionEnvelope transaction, ICollection<TransactionEnvelope> siblings);
    }
}
