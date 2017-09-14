using System.Collections.Generic;
using System.Threading.Tasks;
using NBlockchain.Models;

namespace NBlockchain.Interfaces
{
    public interface IBlockbaseTransactionBuilder
    {
        Task<Transaction> Build(KeyPair builderKeys, ICollection<Transaction> transactions);
    }
}