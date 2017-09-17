using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NBlockchain.Models;

namespace NBlockchain.Interfaces
{
    public interface ITransactionRule
    {
        int Validate(Transaction transaction, ICollection<Transaction> siblings);
    }
}
