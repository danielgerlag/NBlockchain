using System.Collections.Generic;
using NBlockchain.Models;

namespace NBlockchain.Interfaces
{
    public interface IBlockVerifier
    {
        bool Verify(Block block, uint difficulty);

        bool VerifyContentThreshold(ICollection<byte[]> actual, ICollection<byte[]> expected);

        int VerifyTransaction(TransactionEnvelope transaction, ICollection<TransactionEnvelope> siblings);
    }
}