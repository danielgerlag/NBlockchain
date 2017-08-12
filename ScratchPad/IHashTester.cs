namespace NBlockChain.Services
{
    public interface IHashTester
    {
        bool TestHash(byte[] hash, uint difficulty);
    }
}