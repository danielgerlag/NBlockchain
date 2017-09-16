using System.Collections.Generic;
using System.Threading.Tasks;
using NBlockchain.Models;

namespace NBlockchain.Interfaces
{
    public interface IBlockVerifier
    {
        Task<bool> Verify(Block block);

        Task<bool> VerifyTransactions(Block block);

        Task<bool> VerifyBlockRules(Block block, bool tail);

        Task<int> VerifyTransaction(Transaction transaction, ICollection<Transaction> siblings);
    }
}