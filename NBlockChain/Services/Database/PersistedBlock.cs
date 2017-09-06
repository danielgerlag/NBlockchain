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
        }
    }

    public class PersistedTransaction : PersistedEntity<TransactionEnvelope, ObjectId>
    {
        public byte[] BlockId { get; set; }

        public PersistedTransaction()
        {
        }

        public PersistedTransaction(byte[] blockId, TransactionEnvelope txn)
        {
            Entity = txn;
            BlockId = blockId;
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
