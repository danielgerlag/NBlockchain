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

        protected LiteCollection<PersistedBlock> MainChain => _connection.Database.GetCollection<PersistedBlock>("MainChain");
        protected LiteCollection<PersistedBlock> ForkChain => _connection.Database.GetCollection<PersistedBlock>("ForkChain");
        protected LiteCollection<PersistedInstruction> Instructions => _connection.Database.GetCollection<PersistedInstruction>("Instructions");

        public DefaultBlockRepository(ILoggerFactory loggerFactory, IDataConnection connection, IAddressEncoder addressEncoder)
        {
            _connection = connection;
            _addressEncoder = addressEncoder;
            _logger = loggerFactory.CreateLogger<DefaultBlockRepository>();
            
            MainChain.EnsureIndex(x => x.Entity.Header.BlockId, true);
            MainChain.EnsureIndex(x => x.Entity.Header.PreviousBlock, true);
            MainChain.EnsureIndex(x => x.Entity.Header.Height, true);

            ForkChain.EnsureIndex(x => x.Entity.Header.BlockId);
            ForkChain.EnsureIndex(x => x.Entity.Header.PreviousBlock);
            ForkChain.EnsureIndex(x => x.Entity.Header.Height);

            Instructions.EnsureIndex(x => x.BlockId);
            Instructions.EnsureIndex(x => x.TransactionId);
            Instructions.EnsureIndex(x => x.Entity.InstructionId);
            Instructions.EnsureIndex(x => x.Entity.PublicKey);
            Instructions.EnsureIndex(x => x.Statistics.PublicKeyHash);
        }

        public Task AddBlock(Block block)
        {
            var persisted = new PersistedBlock(block);
            var prevHeader = MainChain
                .Find(x => x.Entity.Header.BlockId == block.Header.PreviousBlock)
                .Select(x => x.Entity.Header)
                .FirstOrDefault();

            if (prevHeader != null)
                persisted.Statistics.BlockTime = Convert.ToInt32(TimeSpan.FromTicks(block.Header.Timestamp - prevHeader.Timestamp).TotalSeconds);

            MainChain.Insert(persisted);

            foreach (var txn in block.Transactions)
            {
                var pt = txn.Instructions.Select(ins => new PersistedInstruction(block.Header.BlockId, txn.TransactionId, ins, _addressEncoder.HashPublicKey(ins.PublicKey))).ToList();
                Instructions.InsertBulk(pt);
            }

            return Task.CompletedTask;
        }

        public Task<bool> HaveBlock(byte[] blockId)
        {            
            var result = MainChain.Exists(x => x.Entity.Header.BlockId == blockId);

            if (!result)
                result = ForkChain.Exists(x => x.Entity.Header.BlockId == blockId);

            return Task.FromResult(result);
        }

        public Task<bool> IsEmpty()
        {
            var count = MainChain.Count();
            return Task.FromResult(count == 0);
        }

        public async Task<BlockHeader> GetNewestBlockHeader()
        {
            if (await IsEmpty())
                return null;

            var max = MainChain.Max<uint>(x => x.Entity.Header.Height).AsInt64;
            var block = MainChain.Find(Query.EQ("Entity.Header.Height", max)).First();
            return block?.Entity.Header;
        }

        public Task<Block> GetNextBlock(byte[] prevBlockId)
        {
            var blockHeader = MainChain.FindOne(x => x.Entity.Header.PreviousBlock == prevBlockId);

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
            
            var sample = MainChain.Find(Query.And(Query.LT("Entity.Header.Timestamp", endTicks), Query.GT("Entity.Header.Timestamp", startTicks)));
            if (sample.Count() == 0)
                return Task.FromResult(0);

            var result = Convert.ToInt32(sample.Average(x => x.Statistics.BlockTime));
            return Task.FromResult(result);
        }

        public Task<BlockHeader> GetBlockHeader(byte[] blockId)
        {
            throw new NotImplementedException();
        }

        public Task<Block> GetBlock(byte[] blockId)
        {
            throw new NotImplementedException();
        }

        public Task<BlockHeader> GetMainChainHeader(uint height)
        {
            throw new NotImplementedException();
        }

        public Task<BlockHeader> GetForkHeader(byte[] forkBlockId)
        {
            throw new NotImplementedException();
        }

        public Task AddDetachedBlock(Block block)
        {
            throw new NotImplementedException();
        }

        public Task<BlockHeader> GetDivergentHeader(byte[] forkTipBlockId)
        {
            throw new NotImplementedException();
        }

        public Task RewindChain(byte[] blockId)
        {
            throw new NotImplementedException();
        }

        public Task<ICollection<Block>> GetFork(byte[] forkTipBlockId)
        {
            throw new NotImplementedException();
        }
    }
}