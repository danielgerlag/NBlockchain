using LiteDB;
using NBlockchain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace NBlockchain.Services.Database
{
    public class PersistedBlock : PersistedEntity<BlockInfo, ObjectId, BlockStatistics>
    {
        public PersistedBlock()
        {
        }

        public PersistedBlock(Block block)
        {
            Entity = new BlockInfo(block);
            Statistics = new BlockStatistics();
            Statistics.TimeStamp = new DateTime(block.Header.Timestamp);
        }
    }

    public class PersistedOrphan : PersistedEntity<Block, ObjectId>
    {
        public PersistedOrphan()
        {
        }

        public PersistedOrphan(Block block)
        {
            Entity = block;
        }
    }


    public class BlockInfo
    {
        public BlockHeader Header { get; set; }
        public MerkleNode MerkleRootNode { get; set; }

        public BlockInfo()
        {
        }

        public BlockInfo(Block block)
        {
            Header = block.Header;
            MerkleRootNode = block.MerkleRootNode;
        }
    }
        
}
