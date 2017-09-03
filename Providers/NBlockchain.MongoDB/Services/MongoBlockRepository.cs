using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NBlockchain.MongoDB.Models;
using NBlockchain.Interfaces;
using NBlockchain.Models;

namespace NBlockchain.MongoDB.Services
{
    public class MongoBlockRepository : IBlockRepository
    {
        private readonly IMongoDatabase _database;
        //private readonly AutoResetEvent _cursorEvent = new AutoResetEvent(true);

        public MongoBlockRepository(IMongoDatabase database)
        {
            _database = database;
            EnsureIndexes();
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

        private IMongoCollection<PersistedBlock> Blocks => _database.GetCollection<PersistedBlock>("nbc.blocks");

        public async Task AddBlock(Block block)
        {
            var persisted = new PersistedBlock(block);
            var prevHeader = Blocks
                .Find(x => x.Header.BlockId == block.Header.PreviousBlock)
                .Project(x => x.Header)
                .FirstOrDefault();

            if (prevHeader != null)
                persisted.Statistics.BlockTime = Convert.ToInt32(TimeSpan.FromTicks(block.Header.Timestamp - prevHeader.Timestamp).TotalSeconds);

            Blocks.InsertOne(persisted);
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

        public async Task<int> GetAverageBlockTimeInSecs(DateTime startUtc, DateTime endUtc)
        {            
            var startTicks = startUtc.Ticks;
            var endTicks = endUtc.Ticks;

            var avgQuery = Blocks.Aggregate()
                .Match(x => x.Header.Timestamp > startTicks && x.Header.Timestamp < endTicks && x.Header.Height > 1)
                .Group(new BsonDocument { { "_id", "$item" }, { "avg", new BsonDocument("$avg", "$Statistics.BlockTime") } })
                .SingleOrDefault();

            if (avgQuery != null)
            {
                if (avgQuery.TryGetValue("avg", out var bOut))
                    return Convert.ToInt32(bOut.AsDouble);
            }

            return 0;
        }

        static bool _indexesCreated = false;

        private void EnsureIndexes()
        {
            if (!_indexesCreated)
            {
                Blocks.Indexes.CreateOne(Builders<PersistedBlock>.IndexKeys.Ascending(x => x.Header.BlockId), new CreateIndexOptions() { Background = true, Unique = true });
                Blocks.Indexes.CreateOne(Builders<PersistedBlock>.IndexKeys.Ascending(x => x.Header.Height), new CreateIndexOptions() { Background = true });
                Blocks.Indexes.CreateOne(Builders<PersistedBlock>.IndexKeys.Hashed(x => x.Header.PreviousBlock), new CreateIndexOptions() { Background = true });
                //Blocks.Indexes.CreateOne(Builders<PersistedBlock>.IndexKeys.Ascending(x => x.Transactions.Select(y => y.OriginKey)), new CreateIndexOptions() { Background = true, Name = "idx_origkey" });
                //Blocks.Indexes.CreateOne(Builders<PersistedBlock>.IndexKeys.Hashed(x => x.Transactions.Select(y => y.Originator)), new CreateIndexOptions() { Background = true, Name = "idx_origin" });

                _indexesCreated = true;
            }
        }        
    }
    
}
