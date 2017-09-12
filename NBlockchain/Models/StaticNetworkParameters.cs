using System;
using System.Collections.Generic;
using System.Text;
using NBlockchain.Interfaces;

namespace NBlockchain.Models
{
    public class StaticNetworkParameters : INetworkParameters
    {
        public TimeSpan BlockTime { get; set; }        
        public uint HeaderVersion { get; set; }
    }
}
