using System;
using System.Collections.Generic;
using System.Text;

namespace NBlockChain.Models
{
    public class BlockHeader
    {
        public long Timestamp { get; set; }

        public int Version { get; set; }

        public byte[] PreviousBlock { get; set; }

        public byte[] MerkelRoot { get; set; }

    }
}
