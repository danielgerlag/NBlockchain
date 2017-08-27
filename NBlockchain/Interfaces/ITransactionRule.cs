using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NBlockchain.Models;

namespace NBlockchain.Interfaces
{
    public interface ITransactionRule
    {
        string TransactionType { get; }
        int Validate(TransactionEnvelope transaction, ICollection<TransactionEnvelope> siblings);
    }
}
