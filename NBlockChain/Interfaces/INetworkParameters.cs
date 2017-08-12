using System;

namespace NBlockChain.Interfaces
{
    public interface INetworkParameters
    {
        TimeSpan BlockTime { get; }
        uint Difficulty { get; }
        uint HeaderVersion { get; }
        decimal ExpectedContentThreshold { get; }
    }
}