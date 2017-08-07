using System;
using System.Collections.Generic;
using System.Text;

namespace NBlockChain.Models
{
    public class Block<T>
        where T : AbstractTransaction
    {
        public BlockHeader Header { get; set; }        
        public ICollection<T> Transactions { get; set; } = new HashSet<T>();
        public MerkleNode MerkleRootNode { get; set; }
    }    
}
