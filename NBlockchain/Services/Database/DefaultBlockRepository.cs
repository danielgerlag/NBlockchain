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
        private readonly IAddressEncoder _addressEncoder;

        protected LiteCollection<PersistedBlock> Blocks => _connection.Database.GetCollection<PersistedBlock>("Blocks");
        protected LiteCollection<PersistedInstruction> Instructions => _connection.Database.GetCollection<PersistedInstruction>("Instructions");

        public DefaultBlockRepository(ILoggerFactory loggerFactory, IDataConnection connection, IAddressEncoder addressEncoder)
        {
            _connection = connection;
            _addressEncoder = addressEncoder;
            _logger = loggerFactory.CreateLogger<DefaultBlockRepository>();
            
            Blocks.EnsureIndex(x => x.Entity.Header.BlockId);
            Blocks.EnsureIndex(x => x.Entity.Header.PreviousBlock);
            Blocks.EnsureIndex(x => x.Entity.Header.Height);
            Instructions.EnsureIndex(x => x.BlockId);
            Instructions.EnsureIndex(x => x.TransactionId);
            Instructions.EnsureIndex(x => x.Entity.InstructionId);
            Instructions.EnsureIndex(x => x.Entity.PublicKey);
            Instructions.EnsureIndex(x => x.Statistics.PublicKeyHash);
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

            foreach (var txn in block.Transactions)
            {
                var pt = txn.Instructions.Select(ins => new PersistedInstruction(block.Header.BlockId, txn.TransactionId, ins, _addressEncoder.HashPublicKey(ins.PublicKey))).ToList();
                Instructions.InsertBulk(pt);
            }

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
            return block?.Entity.Header;
        }

        public Task<Block> GetNextBlock(byte[] prevBlockId)
        {
            var blockHeader = Blocks.FindOne(x => x.Entity.Header.PreviousBlock == prevBlockId);

            if (blockHeader == null)
                return Task.FromResult<Block>(null);

            var result = new Block();
            result.Header = blockHeader.Entity.Header;
            result.MerkleRootNode = blockHeader.Entity.MerkleRootNode;

            var instructions = Instructions.Find(Query.EQ("BlockId", blockHeader.Entity.Header.BlockId));
            result.Transactions = instructions
                .GroupBy(x => x.TransactionId, new ByteArrayEqualityComparer())
                .Select(x =>  new Transaction(x.Select(y => y.Entity).ToList()) { TransactionId = x.Key })
                .ToList();

            return Task.FromResult(result);
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