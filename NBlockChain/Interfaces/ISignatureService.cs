using NBlockChain.Models;

namespace NBlockChain.Interfaces
{
    public interface ISignatureService
    {
        KeyPair GenerateKeyPair();
        void SignTransaction(TransactionEnvelope transaction, byte[] privateKey);
        bool VerifyTransaction(TransactionEnvelope transaction);
    }
}