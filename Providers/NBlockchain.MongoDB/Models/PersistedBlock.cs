using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Bson;
using NBlockchain.Interfaces;
using NBlockchain.Models;
using NBlockchain.Services.Database;

namespace NBlockchain.MongoDB.Models
{
    public class PersistedBlock
    {
        public ObjectId Id { get; set; }
        public BlockStatistics Statistics { get; set; } = new BlockStatistics();

        public BlockHeader Header { get; set; }
        public ICollection<PersistedTransaction> Transactions { get; set; } = new HashSet<PersistedTransaction>();
        public MerkleNode MerkleRootNode { get; set; }


        public PersistedBlock()
        {
        }

        public PersistedBlock(Block block, IAddressEncoder addressEncoder)
        {
            Header = block.Header;
            MerkleRootNode = block.MerkleRootNode;
            Statistics.TimeStamp = new DateTime(block.Header.Timestamp);

            foreach (var txn in block.Transactions)
                Transactions.Add(new PersistedTransaction(txn, addressEncoder));
        }

        public Block ToBlock()
        {
            var result = new Block();
            result.Header = Header;
            result.MerkleRootNode = MerkleRootNode;

            foreach (var txn in Transactions)
                result.Transactions.Add(txn.ToTransaction());

            return result;
        }

    }
}
