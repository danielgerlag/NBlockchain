using System.Collections.Generic;
using NBlockchain.Models;

namespace NBlockchain.Interfaces
{
    public interface IBlockVerifier
    {
        bool Verify(Block block);

        bool VerifyBlockRules(Block block, bool tail);

        int VerifyTransaction(TransactionEnvelope transaction, ICollection<TransactionEnvelope> siblings);
    }
}