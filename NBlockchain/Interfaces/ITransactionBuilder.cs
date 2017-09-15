using System.Collections.Generic;
using System.Threading.Tasks;
using NBlockchain.Models;

namespace NBlockchain.Interfaces
{
    public interface ITransactionBuilder
    {
        Task<Transaction> Build(ICollection<Instruction> instructions);
    }
}