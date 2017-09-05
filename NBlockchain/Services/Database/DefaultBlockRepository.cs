using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using Microsoft.Extensions.Logging;
using NBlockchain.Interfaces;
using NBlockchain.Models;
using System.Linq;

namespace NBlockchain.Services.Database
{
    public class DefaultBlockRepository : IBlockRepository
    {
        private readonly ILogger _logger;
        private readonly IDataConnection _connection;

        protected LiteCollection<PersistedBlock> Blocks => _connection.Database.GetCollection<PersistedBlock>("Blocks");

        public DefaultBlockRepository(ILoggerFactory loggerFactory, IDataConnection connection)
        {
            _connection = connection;
            _logger = loggerFactory.CreateLogger<DefaultBlockRepository>();
            
            Blocks.EnsureIndex(x => x.Entity.Header.BlockId);
            Blocks.EnsureIndex(x => x.Entity.Header.PreviousBlock);
            Blocks.EnsureIndex(x => x.Entity.Header.Height);
        }

        public Task AddBlock(Block block)
        {
            var persisted = new PersistedBlock(block);
            var prevHeader = Blocks
                .Find(x => x.Entity.Header.BlockId == block.Header.PreviousBlock)
                .Select(x => x.Entity.Header)
                .FirstOrDefault();

            if (prevHeader != null)
                persisted.Statistics.BlockTime = Convert.ToInt32(TimeSpan.FromTicks(block.Header.Timestamp - prevHeader.Timestamp).TotalSeconds);

            Blocks.Insert(persisted);
            return Task.CompletedTask;
        }

        public Task<bool> HaveBlock(byte[] blockId)
        {            
            var result = Blocks.Exists(x => x.Entity.Header.BlockId == blockId);
            return Task.FromResult(result);
        }

        public Task<bool> IsEmpty()
        {
            var count = Blocks.Count();
            return Task.FromResult(count == 0);
        }

        public async Task<BlockHeader> GetNewestBlockHeader()
        {
            if (await IsEmpty())
                return null;

            var max = Blocks.Max<uint>(x => x.Entity.Header.Height).AsInt64;
            var block = Blocks.Find(Query.EQ("Entity.Header.Height", max)).First();
            return await Task.FromResult(block?.Entity.Header);
        }

        public Task<Block> GetNextBlock(byte[] prevBlockId)
        {
            var block = Blocks.FindOne(x => x.Entity.Header.PreviousBlock == prevBlockId);
            return Task.FromResult(block?.Entity);
        }
        
        public Task<int> GetAverageBlockTimeInSecs(DateTime startUtc, DateTime endUtc)
        {
            var startTicks = startUtc.Ticks;
            var endTicks = endUtc.Ticks;
            
            var sample = Blocks.Find(Query.And(Query.LT("Entity.Header.Timestamp", endTicks), Query.GT("Entity.Header.Timestamp", startTicks)));
            if (sample.Count() == 0)
                return Task.FromResult(0);

            var result = Convert.ToInt32(sample.Average(x => x.Statistics.BlockTime));
            return Task.FromResult(result);
        }
    }
}