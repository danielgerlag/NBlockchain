using NBlockChain.Models;

namespace NBlockChain.Interfaces
{
    public interface ISignatureService
    {
        KeyPair GenerateKeyPair();
        byte[] SignData(byte[] data, byte[] privateKey);
        bool VerifyData(byte[] data, byte[] signature, byte[] publicKey);
    }
}