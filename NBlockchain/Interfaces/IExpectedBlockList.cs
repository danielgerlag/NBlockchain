namespace NBlockchain.Interfaces
{
    public interface IExpectedBlockList
    {
        void Confirm(byte[] previousId);
        void ExpectNext(byte[] previousId);
        bool IsExpected(byte[] previousId);
    }
}