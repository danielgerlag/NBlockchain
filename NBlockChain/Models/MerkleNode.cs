using System;
using System.Collections.Generic;
using System.Text;

namespace NBlockchain.Models
{
    public class MerkleNode
    {

        public byte[] Value { get; set; }

        public MerkleNode Left { get; set; }

        public MerkleNode Right { get; set; }

    }
}
