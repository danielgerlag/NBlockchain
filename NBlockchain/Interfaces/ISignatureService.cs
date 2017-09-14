using NBlockchain.Models;

namespace NBlockchain.Interfaces
{
    public interface ISignatureService
    {
        KeyPair GenerateKeyPair();
        void SignInstruction(Instruction instruction, byte[] privateKey);
        bool VerifyInstruction(Instruction instruction);
    }
}