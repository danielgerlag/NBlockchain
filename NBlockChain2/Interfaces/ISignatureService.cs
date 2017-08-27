using NBlockchain.Models;

namespace NBlockchain.Interfaces
{
    public interface ISignatureService
    {
        KeyPair GenerateKeyPair();
        void SignTransaction(TransactionEnvelope transaction, byte[] privateKey);
        bool VerifyTransaction(TransactionEnvelope transaction);
    }
}