namespace NBlockChain.Interfaces
{
    public interface IBlockIntervalCalculator
    {
        uint HeightNow { get; }

        uint DetermineHeight(long now);
    }
}