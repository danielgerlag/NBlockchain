using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Bson;
using NBlockchain.Models;

namespace NBlockchain.MongoDB.Models
{
    public class PersistedBlock : Block
    {
        public ObjectId Id { get; set; }

        public BlockStatistics Statistics { get; set; } = new BlockStatistics();

        public PersistedBlock()
        {
        }

        public PersistedBlock(Block block)
        {
            this.Header = block.Header;
            this.MerkleRootNode = block.MerkleRootNode;
            this.Transactions = block.Transactions;
        }
    }
}
