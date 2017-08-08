using System;
using System.Collections.Generic;
using System.Text;

namespace NBlockChain.Models
{
    public abstract class AbstractTransaction
    {
        public long Timestamp { get; set; }

        public uint Version { get; set; }

        public byte[] Originator { get; set; }

        public byte[] Signature { get; set; }

        public abstract byte[] GetRawData();

    }
}
