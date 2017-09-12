using System;

namespace NBlockchain.Interfaces
{
    public interface INetworkParameters
    {
        TimeSpan BlockTime { get; }        
        uint HeaderVersion { get; }
    }
}