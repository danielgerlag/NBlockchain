using System;
using System.Collections.Generic;
using System.Text;

namespace NBlockChain.Models
{
    public abstract class AbstractEvent
    {
        public long Timestamp { get; set; }

        public abstract byte[] GetRawData();

    }
}
