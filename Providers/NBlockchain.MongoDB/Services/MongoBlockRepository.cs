using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using NBlockchain.MongoDB.Models;
using NBlockchain.Interfaces;
using NBlockchain.Models;
using System.IO;

namespace NBlockchain.MongoDB.Services
{
    public class MongoBlockRepository : IBlockRepository
    {
        private readonly IMongoDatabase _database;
        private readonly IAddressEncoder _addressEncoder;

        public MongoBlockRepository(IMongoDatabase database, IAddressEncoder addressEncoder)
        {
            _database = database;
            _addressEncoder = addressEncoder;
            EnsureIndexes();
        }

        static MongoBlockRepository()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in assemblies)
            {
                foreach (var type in asm.ExportedTypes)
                {
                    var attr = type.GetTypeInfo().GetCustomAttribute<InstructionTypeAttribute>();
                    if (attr != null)
                    {
                        BsonSerializer.RegisterDiscriminator(type, new BsonString(attr.TypeIdentifier));
                        //BsonSerializer.RegisterDiscriminator(type, TypeNameDiscriminator.GetDiscriminator(type));

                        //hack for dodgy mongo driver
                        IBsonWriter w = new BsonBinaryWriter(new MemoryStream());
                        var constr = type.GetConstructor(new Type[0]);                        
                        BsonSerializer.Serialize(w, type, constr.Invoke(null));
                    }
                }
            }

            //BsonSerializer.RegisterDiscriminatorConvention(typeof(BlockTransaction), StandardDiscriminatorConvention.Scalar);

            
            //TypeNameDiscriminator.GetDiscriminator
            //TypeNameDiscriminator.
            //BsonSerializer.RegisterDiscriminatorConvention(typeof(object), ObjectDiscriminatorConvention.Instance);
            //BsonSerializer.RegisterDiscriminator(t, t.FullName));
        }

        private IMongoCollection<PersistedBlock> Blocks => _database.GetCollection<PersistedBlock>("nbc.blocks");

        public async Task AddBlock(Block block)
        {
            var persisted = new PersistedBlock(block, _addressEncoder);
            var prevHeader = Blocks
                .Find(x => x.Header.BlockId == block.Header.PreviousBlock)
                .Project(x => x.Header)
                .FirstOrDefault();
            
            if (prevHeader != null)
                persisted.Statistics.BlockTime = Convert.ToInt32(TimeSpan.FromTicks(block.Header.Timestamp - prevHeader.Timestamp).TotalSeconds);

            Blocks.InsertOne(persisted);

            await Task.Yield();
        }

        public Task<bool> HaveBlock(byte[] blockId)
        {            
            var query = Blocks.Find(x => x.Header.BlockId == blockId);
            return Task.FromResult(query.Any());
        }

        public Task<bool> IsEmpty()
        {
            return Task.FromResult(Blocks.Count(x => true) == 0);
        }

        public Task<BlockHeader> GetNewestBlockHeader()
        {
            if (Blocks.Count(x => true) == 0)
                return Task.FromResult<BlockHeader>(null);

            var height = Blocks.AsQueryable().Max(x => x.Header.Height);
            var query = Blocks.AsQueryable().Select(x => x.Header).Where(x => x.Height == height);
            var result = query.FirstOrDefault();
            return Task.FromResult(result);
        }

        public Task<Block> GetNextBlock(byte[] prevBlockId)
        {
            var query = Blocks.Find(x => x.Header.PreviousBlock == prevBlockId);
            var persistedResult = query.FirstOrDefault();
            return persistedResult == null ? Task.FromResult<Block>(null) : Task.FromResult(persistedResult.ToBlock());
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

                //?
                
                Blocks.Indexes.CreateOne(Builders<PersistedBlock>.IndexKeys.Ascending(new StringFieldDefinition<PersistedBlock>("Transactions.TransactionId")), new CreateIndexOptions() { Background = true });
                Blocks.Indexes.CreateOne(Builders<PersistedBlock>.IndexKeys.Ascending(new StringFieldDefinition<PersistedBlock>("Transactions.Instructions.Id")), new CreateIndexOptions() { Background = true });

                _indexesCreated = true;
            }
        }        
    }
    
}
