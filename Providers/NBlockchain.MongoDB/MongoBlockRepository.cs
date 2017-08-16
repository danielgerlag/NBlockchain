using System;
using System.Threading.Tasks;
using System.Linq;
using MongoDB.Driver;
using NBlockChain.Interfaces;
using NBlockChain.Models;

namespace NBlockchain.MongoDB
{
    public class MongoBlockRepository : IBlockRepository
    {
        private readonly IMongoDatabase _database;

        public MongoBlockRepository(IMongoDatabase database)
        {
            _database = database;
            //CreateIndexes(this);
        }

        private IMongoCollection<Block> Blocks => _database.GetCollection<Block>("nbc.blocks");

        public async Task AddBlock(Block block)
        {
            await Blocks.InsertOneAsync(block);
        }

        public async Task<bool> HaveBlock(byte[] blockId)
        {
            var query = Blocks.Find(x => x.Header.BlockId == blockId);
            return await query.AnyAsync();
        }

        public async Task<bool> IsEmpty()
        {
            return (await Blocks.CountAsync(x => true) == 0);
        }

        public async Task<BlockHeader> GetNewestBlockHeader()
        {
            if (await IsEmpty())
                return null;

            var height = Blocks.AsQueryable().Max(x => x.Header.Height);
            var query = Blocks.AsQueryable().Select(x => x.Header).Where(x => x.Height == height);
            return query.FirstOrDefault();
        }

        public async Task<Block> GetNextBlock(byte[] prevBlockId)
        {
            var query = Blocks.Find(x => x.Header.PreviousBlock == prevBlockId);
            return await query.FirstOrDefaultAsync();
        }

        public async Task<long> GetGenesisBlockTime()
        {
            if (await IsEmpty())
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
}
