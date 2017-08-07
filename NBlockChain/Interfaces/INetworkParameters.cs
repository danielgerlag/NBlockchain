using System;

namespace NBlockChain.Interfaces
{
    public interface INetworkParameters
    {
        TimeSpan BlockTime { get; }
        uint TransactionVersion { get; }
        uint Difficulty { get; }
    }
}