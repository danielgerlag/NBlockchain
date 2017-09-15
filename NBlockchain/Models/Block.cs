using System;
using System.Collections.Generic;
using System.Text;

namespace NBlockchain.Models
{
    public class Block
    {
        public static byte[] HeadKey = new byte[] { 0x0 };

        public BlockHeader Header { get; set; }
        public ICollection<Transaction> Transactions { get; set; } = new HashSet<Transaction>();
        public MerkleNode MerkleRootNode { get; set; }

        public Block()
        {
            Header = new BlockHeader();
        }
    }    
}
