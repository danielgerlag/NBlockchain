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
using System.Collections.Generic;

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

        private IMongoCollection<PersistedBlock> MainChain => _database.GetCollection<PersistedBlock>("MainChain");
        private IMongoCollection<PersistedBlock> ForkChain => _database.GetCollection<PersistedBlock>("ForkChain");

        public async Task AddBlock(Block block)
        {
            var persisted = new PersistedBlock(block, _addressEncoder);
            var prevHeader = MainChain
                .Find(x => x.Header.BlockId == block.Header.PreviousBlock)
                .Project(x => x.Header)
                .FirstOrDefault();
            
            if (prevHeader != null)
                persisted.Statistics.BlockTime = Convert.ToInt32(TimeSpan.FromTicks(block.Header.Timestamp - prevHeader.Timestamp).TotalSeconds);

            MainChain.InsertOne(persisted);
            ForkChain.DeleteMany(x => x.Header.BlockId == block.Header.BlockId);

            await Task.Yield();
        }

        public Task<bool> HavePrimaryBlock(byte[] blockId)
        {
            var result = MainChain.Find(x => x.Header.BlockId == blockId).Any();
            return Task.FromResult(result);
        }

        public Task<bool> HaveSecondaryBlock(byte[] blockId)
        {
            var result = ForkChain.Find(x => x.Header.BlockId == blockId).Any();
            return Task.FromResult(result);
        }

        public Task<bool> IsEmpty()
        {
            return Task.FromResult(MainChain.Count(x => true) == 0);
        }

        public Task<BlockHeader> GetBestBlockHeader()
        {
            if (MainChain.Count(x => true) == 0)
                return Task.FromResult<BlockHeader>(null);

            var height = MainChain.AsQueryable().Max(x => x.Header.Height);
            var query = MainChain.AsQueryable().Select(x => x.Header).Where(x => x.Height == height);
            var result = query.FirstOrDefault();
            return Task.FromResult(result);
        }

        public Task<Block> GetNextBlock(byte[] prevBlockId)
        {
            var persistedResult = MainChain.Find(x => x.Header.PreviousBlock == prevBlockId).FirstOrDefault();
            
            if (persistedResult == null)
                persistedResult = ForkChain.Find(x => x.Header.PreviousBlock == prevBlockId).FirstOrDefault();

            return persistedResult == null ? Task.FromResult<Block>(null) : Task.FromResult(persistedResult.ToBlock());
        }

        public Task DiscardSecondaryBlock(byte[] blockId)
        {
            ForkChain.DeleteMany(x => x.Header.BlockId == blockId);
            return Task.CompletedTask;
        }

        public async Task<int> GetAverageBlockTimeInSecs(DateTime startUtc, DateTime endUtc)
        {            
            var startTicks = startUtc.Ticks;
            var endTicks = endUtc.Ticks;

            var avgQuery = MainChain.Aggregate()
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
        
        public Task<BlockHeader> GetBlockHeader(byte[] blockId)
        {
            if (MainChain.Count(x => true) == 0)
                return Task.FromResult<BlockHeader>(null);
                        
            var result = MainChain.AsQueryable().Select(x => x.Header).Where(x => x.BlockId == blockId).FirstOrDefault();

            if (result == null)
                result = ForkChain.AsQueryable().Select(x => x.Header).Where(x => x.BlockId == blockId).FirstOrDefault();

            return Task.FromResult(result);
        }

        public Task<Block> GetBlock(byte[] blockId)
        {
            var persistedResult = MainChain.Find(x => x.Header.BlockId == blockId).FirstOrDefault();
            
            if (persistedResult == null)
                persistedResult = ForkChain.Find(x => x.Header.BlockId == blockId).FirstOrDefault();

            return persistedResult == null ? Task.FromResult<Block>(null) : Task.FromResult(persistedResult.ToBlock());
        }

        public Task<BlockHeader> GetPrimaryHeader(uint height)
        {
            if (MainChain.Count(x => true) == 0)
                return Task.FromResult<BlockHeader>(null);

            var query = MainChain.AsQueryable().Select(x => x.Header).Where(x => x.Height == height);
            var result = query.FirstOrDefault();
            return Task.FromResult(result);
        }

        public async Task AddSecondaryBlock(Block block)
        {
            var persisted = new PersistedBlock(block, _addressEncoder);
            ForkChain.InsertOne(persisted);
            await Task.Yield();
        }

        public Task<BlockHeader> GetSecondaryHeader(byte[] forkBlockId)
        {
            var forktip = ForkChain.AsQueryable().Select(x => x.Header).Where(x => x.BlockId == forkBlockId).FirstOrDefault();
            return Task.FromResult(forktip);
        }
                
        public async Task<BlockHeader> GetDivergentHeader(byte[] forkTipBlockId)
        {
            var forkHeader = await GetSecondaryHeader(forkTipBlockId);
            if (forkHeader == null)
                return null;

            while (!forkHeader.PreviousBlock.SequenceEqual(Block.HeadKey))
            {
                var mainParent = MainChain.AsQueryable().Select(x => x.Header).Where(x => x.BlockId == forkHeader.PreviousBlock).FirstOrDefault();
                if (mainParent != null)
                    return mainParent;

                forkHeader = await GetSecondaryHeader(forkHeader.PreviousBlock);
                if (forkHeader == null)
                    return null;
            }
            return null;
        }

        public async Task RewindChain(byte[] blockId)
        {
            var divergent = MainChain.AsQueryable().FirstOrDefault(x => x.Header.BlockId == blockId);
            if (divergent == null)
                return;

            var archiveFork = MainChain.AsQueryable()
                .Where(x => x.Header.Height > divergent.Header.Height)
                .ToList()
                .Select(x => x.ToBlock());

            foreach (var block in archiveFork)
                await AddSecondaryBlock(block);

            MainChain.DeleteMany(x => x.Header.Height > divergent.Header.Height);
        }

        public async Task<ICollection<Block>> GetFork(byte[] forkTipBlockId)
        {
            var result = new List<Block>();

            var forkBlock = ForkChain.AsQueryable().FirstOrDefault(x => x.Header.BlockId == forkTipBlockId);
            if (forkBlock == null)
                return result;

            while (!forkBlock.Header.PreviousBlock.SequenceEqual(Block.HeadKey))
            {
                result.Add(forkBlock.ToBlock());
                var mainParent = MainChain.AsQueryable().Select(x => x.Header).Where(x => x.BlockId == forkBlock.Header.PreviousBlock).FirstOrDefault();
                if (mainParent != null)
                    break;

                forkBlock = ForkChain.AsQueryable().FirstOrDefault(x => x.Header.BlockId == forkBlock.Header.PreviousBlock);
                if (forkBlock == null)
                    break;
            }

            return result.OrderBy(x => x.Header.Height).ToList();
        }

        static bool _indexesCreated = false;

        private void EnsureIndexes()
        {
            if (!_indexesCreated)
            {
                MainChain.Indexes.CreateOne(Builders<PersistedBlock>.IndexKeys.Ascending(x => x.Header.BlockId), new CreateIndexOptions() { Background = true, Unique = true });
                MainChain.Indexes.CreateOne(Builders<PersistedBlock>.IndexKeys.Ascending(x => x.Header.Height), new CreateIndexOptions() { Background = true, Unique = true });
                MainChain.Indexes.CreateOne(Builders<PersistedBlock>.IndexKeys.Ascending(x => x.Header.PreviousBlock), new CreateIndexOptions() { Background = true, Unique = true });
                MainChain.Indexes.CreateOne(Builders<PersistedBlock>.IndexKeys.Ascending(new StringFieldDefinition<PersistedBlock>("Transactions.TransactionId")), new CreateIndexOptions() { Background = true });
                MainChain.Indexes.CreateOne(Builders<PersistedBlock>.IndexKeys.Ascending(new StringFieldDefinition<PersistedBlock>("Transactions.Instructions.Id")), new CreateIndexOptions() { Background = true });

                ForkChain.Indexes.CreateOne(Builders<PersistedBlock>.IndexKeys.Ascending(x => x.Header.BlockId), new CreateIndexOptions() { Background = true });
                ForkChain.Indexes.CreateOne(Builders<PersistedBlock>.IndexKeys.Ascending(x => x.Header.Height), new CreateIndexOptions() { Background = true });
                ForkChain.Indexes.CreateOne(Builders<PersistedBlock>.IndexKeys.Ascending(x => x.Header.PreviousBlock), new CreateIndexOptions() { Background = true });

                _indexesCreated = true;
            }
        }
    }
    
}
