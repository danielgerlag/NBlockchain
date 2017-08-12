using System;

namespace NBlockChain.Interfaces
{
    public interface IBlockIntervalCalculator
    {
        uint HeightNow { get; }

        uint DetermineHeight(long now);

        long NextBlockTime { get; }

        long LastBlockTime { get; }

        TimeSpan TimeUntilNextBlock { get; }
    }
}