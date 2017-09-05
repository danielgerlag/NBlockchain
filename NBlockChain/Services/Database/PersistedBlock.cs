using LiteDB;
using NBlockchain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace NBlockchain.Services.Database
{
    public class PersistedBlock : PersistedEntity<Block, ObjectId, BlockStatistics>
    {
        public PersistedBlock()
        {
        }

        public PersistedBlock(Block block)
        {
            Entity = block;
            Statistics = new BlockStatistics();
        }
    }
}
