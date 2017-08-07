using NBlockChain.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NBlockChain.Interfaces
{
    public interface ITransactionValidator<T>
        where T : AbstractTransaction
    {
        Task<int> Validate(T transaction);
    }
}
