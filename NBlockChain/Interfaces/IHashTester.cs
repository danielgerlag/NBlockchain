namespace NBlockChain.Interfaces
{
    public interface IHashTester
    {
        bool TestHash(byte[] hash, uint difficulty);
    }
}