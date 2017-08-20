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
        //private readonly AutoResetEvent _cursorEvent = new AutoResetEvent(true);

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
            Blocks.InsertOne(new MongoBlock(block));            
        }

        public async Task<bool> HaveBlock(byte[] blockId)
        {            
            var query = Blocks.Find(x => x.Header.BlockId == blockId);
            return query.Any();
        }

        public async Task<bool> IsEmpty()
        {
            return (Blocks.Count(x => true) == 0);
        }

        public async Task<BlockHeader> GetNewestBlockHeader()
        {
            if (Blocks.Count(x => true) == 0)
                return null;

            var height = Blocks.AsQueryable().Max(x => x.Header.Height);
            var query = Blocks.AsQueryable().Select(x => x.Header).Where(x => x.Height == height);
            return query.FirstOrDefault();
        }

        public async Task<Block> GetNextBlock(byte[] prevBlockId)
        {
            var query = Blocks.Find(x => x.Header.PreviousBlock == prevBlockId);
            return query.FirstOrDefault();
        }

        public async Task<long> GetGenesisBlockTime()
        {
            if (Blocks.Count(x => true) == 0)
                return DateTime.UtcNow.Ticks;

            return Blocks.AsQueryable().Min(x => x.Header.Timestamp);
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
