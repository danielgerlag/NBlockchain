using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using NBlockChain.Interfaces;
using NBlockChain.Models;
using Newtonsoft.Json.Linq;

namespace NBlockchain.MongoDB
{
    public class MongoBlockRepository : IBlockRepository
    {
        private readonly IMongoDatabase _database;
        private readonly AutoResetEvent _cursorEvent = new AutoResetEvent(true);

        public MongoBlockRepository(IMongoDatabase database)
        {
            _database = database;
            //CreateIndexes(this);
        }

        static MongoBlockRepository()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in assemblies)
            {
                foreach (var type in asm.ExportedTypes)
                {
                    var attr = type.GetTypeInfo().GetCustomAttribute<TransactionTypeAttribute>();
                    if (attr != null)
                    {
                        BsonSerializer.RegisterDiscriminator(type, new BsonString(attr.TypeId));
                    }
                }
            }
            //BsonSerializer.RegisterDiscriminator(t, t.FullName));
        }

        private IMongoCollection<MongoBlock> Blocks => _database.GetCollection<MongoBlock>("nbc.blocks");

        public async Task AddBlock(Block block)
        {
            _cursorEvent.WaitOne();
            try
            {
                Blocks.InsertOne(new MongoBlock(block));
            }
            finally
            {
                _cursorEvent.Set();
            }
        }

        public async Task<bool> HaveBlock(byte[] blockId)
        {
            _cursorEvent.WaitOne();
            try
            {
                var query = Blocks.Find(x => x.Header.BlockId == blockId);
                return query.Any();
            }
            finally
            {
                _cursorEvent.Set();
            }
        }

        public async Task<bool> IsEmpty()
        {
            _cursorEvent.WaitOne();
            try
            {
                return (Blocks.Count(x => true) == 0);
            }
            finally
            {
                _cursorEvent.Set();
            }
        }

        public async Task<BlockHeader> GetNewestBlockHeader()
        {
            _cursorEvent.WaitOne();
            try
            {
                if (Blocks.Count(x => true) == 0)
                    return null;

                var height = Blocks.AsQueryable().Max(x => x.Header.Height);
                var query = Blocks.AsQueryable().Select(x => x.Header).Where(x => x.Height == height);
                return query.FirstOrDefault();
            }
            finally
            {
                _cursorEvent.Set();
            }
        }

        public async Task<Block> GetNextBlock(byte[] prevBlockId)
        {
            _cursorEvent.WaitOne();
            try
            {
                var query = Blocks.Find(x => x.Header.PreviousBlock == prevBlockId);
                return query.FirstOrDefault();
            }
            finally
            {
                _cursorEvent.Set();
            }
        }

        public async Task<long> GetGenesisBlockTime()
        {
            _cursorEvent.WaitOne();
            try
            {
                if (Blocks.Count(x => true) == 0)
                    return DateTime.UtcNow.Ticks;

                return Blocks.AsQueryable().Min(x => x.Header.Timestamp);
            }
            finally
            {
                _cursorEvent.Set();
            }
        }

        static bool indexesCreated = false;
        static void CreateIndexes(MongoBlockRepository instance)
        {
            if (!indexesCreated)
            {
                //TODO
                indexesCreated = true;
            }
        }
    }

    public class MongoBlock : Block
    {
        public ObjectId Id { get; set; }

        public MongoBlock()
        {
        }

        public MongoBlock(Block block)
        {
            this.Header = block.Header;
            this.MerkleRootNode = block.MerkleRootNode;
            this.Transactions = block.Transactions;
        }
    }
}
