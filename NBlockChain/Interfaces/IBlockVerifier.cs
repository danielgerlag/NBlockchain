using NBlockChain.Models;
using System.Collections.Generic;

namespace NBlockChain.Interfaces
{
    public interface IBlockVerifier
    {
        bool Verify(Block block);

        bool VerifyContentThreshold(ICollection<byte[]> actual, ICollection<byte[]> expected);

        int VerifyTransaction(TransactionEnvelope transaction);
    }
}