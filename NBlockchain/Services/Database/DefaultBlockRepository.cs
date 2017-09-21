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
        protected LiteCollection<PersistedOrphan> ForkChain => _connection.Database.GetCollection<PersistedOrphan>("ForkChain");
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

            ForkChain.Delete(x => x.Entity.Header.BlockId == block.Header.BlockId);

            return Task.CompletedTask;
        }

        public Task<bool> HavePrimaryBlock(byte[] blockId)
        {            
            var result = MainChain.Exists(x => x.Entity.Header.BlockId == blockId);
            return Task.FromResult(result);
        }

        public Task<bool> IsEmpty()
        {
            var count = MainChain.Count();
            return Task.FromResult(count == 0);
        }

        public async Task<BlockHeader> GetBestBlockHeader()
        {
            if (await IsEmpty())
                return null;

            var max = MainChain.Max<uint>(x => x.Entity.Header.Height).AsInt64;
            var block = MainChain.Find(Query.EQ("Entity.Header.Height", max)).FirstOrDefault();
            return block?.Entity.Header;
        }

        public Task<Block> GetNextBlock(byte[] prevBlockId)
        {
            var persistedBlock = MainChain.FindOne(x => x.Entity.Header.PreviousBlock == prevBlockId);

            if (persistedBlock == null)
            {
                var forkBlock = ForkChain.FindOne(x => x.Entity.Header.PreviousBlock == prevBlockId);
                return Task.FromResult(forkBlock?.Entity);
            }
            var result = RehydratePersistedBlock(persistedBlock);

            return Task.FromResult(result);
        }

        private Block RehydratePersistedBlock(PersistedBlock persistedBlock)
        {
            var result = new Block();
            result.Header = persistedBlock.Entity.Header;
            result.MerkleRootNode = persistedBlock.Entity.MerkleRootNode;

            var instructions = Instructions.Find(Query.EQ("BlockId", persistedBlock.Entity.Header.BlockId));
            result.Transactions = instructions
                .GroupBy(x => x.TransactionId, new ByteArrayEqualityComparer())
                .Select(x => new Transaction(x.Select(y => y.Entity).ToList()) { TransactionId = x.Key })
                .ToList();

            return result;
        }

        public Task DiscardSecondaryBlock(byte[] blockId)
        {
            ForkChain.Delete(x => x.Entity.Header.BlockId == blockId);
            return Task.CompletedTask;
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

        public async Task<BlockHeader> GetBlockHeader(byte[] blockId)
        {
            var block = MainChain.Find(Query.EQ("Entity.Header.BlockId", blockId)).FirstOrDefault();
            if (block == null)
            {
                var fork = ForkChain.Find(Query.EQ("Entity.Header.BlockId", blockId)).FirstOrDefault();
                return fork?.Entity.Header;
            }

            return block?.Entity.Header;
        }

        public async Task<Block> GetBlock(byte[] blockId)
        {
            var block = MainChain.Find(Query.EQ("Entity.Header.BlockId", blockId)).FirstOrDefault();
            if (block == null)
            {
                var fork = ForkChain.Find(Query.EQ("Entity.Header.BlockId", blockId)).FirstOrDefault();
                return fork?.Entity;
            }

            return RehydratePersistedBlock(block);
        }

        public async Task<BlockHeader> GetPrimaryHeader(uint height)
        {
            //TODO: project result
            var block = MainChain.Find(Query.EQ("Entity.Header.Height", Convert.ToInt64(height))).FirstOrDefault();
            return block?.Entity.Header;
        }

        public async Task<BlockHeader> GetSecondaryHeader(byte[] forkBlockId)
        {
            //TODO: project result
            var fork = ForkChain.Find(Query.EQ("Entity.Header.BlockId", forkBlockId)).FirstOrDefault();
            return fork?.Entity.Header;
        }

        public async Task AddSecondaryBlock(Block block)
        {
            ForkChain.Insert(new PersistedOrphan(block));
        }

        public async Task<BlockHeader> GetDivergentHeader(byte[] forkTipBlockId)
        {
            var forkHeader = await GetSecondaryHeader(forkTipBlockId);
            if (forkHeader == null)
                return null;

            while (!forkHeader.PreviousBlock.SequenceEqual(Block.HeadKey))
            {
                var mainParent = MainChain.Find(x => x.Entity.Header.BlockId == forkHeader.PreviousBlock).FirstOrDefault();
                if (mainParent != null)
                    return mainParent.Entity.Header;

                forkHeader = await GetSecondaryHeader(forkHeader.PreviousBlock);
                if (forkHeader == null)
                    return null;
            }
            return null;
        }

        public async Task RewindChain(byte[] blockId)
        {
            var divergent = MainChain.Find(x => x.Entity.Header.BlockId == blockId).FirstOrDefault();
            if (divergent == null)
                return;

            var archiveFork = MainChain
                .Find(x => x.Entity.Header.Height > divergent.Entity.Header.Height)
                .ToList()
                .Select(RehydratePersistedBlock);

            foreach (var block in archiveFork.OrderByDescending(x => x.Header.Height))
            {
                await AddSecondaryBlock(block);
                Instructions.Delete(x => x.BlockId == block.Header.BlockId);
                MainChain.Delete(x => x.Entity.Header.BlockId == block.Header.BlockId);
            }
        }

        public async Task<ICollection<Block>> GetFork(byte[] forkTipBlockId)
        {
            var result = new List<Block>();

            var forkBlock = ForkChain.Find(x => x.Entity.Header.BlockId == forkTipBlockId).FirstOrDefault();
            if (forkBlock == null)
                return result;

            while (!forkBlock.Entity.Header.PreviousBlock.SequenceEqual(Block.HeadKey))
            {
                result.Add(forkBlock.Entity);
                var mainParent = MainChain.Find(x => x.Entity.Header.BlockId == forkBlock.Entity.Header.PreviousBlock).FirstOrDefault();
                if (mainParent != null)
                    break;

                forkBlock = ForkChain.Find(x => x.Entity.Header.BlockId == forkBlock.Entity.Header.PreviousBlock).FirstOrDefault();
                if (forkBlock == null)
                    break;
            }

            return result.OrderBy(x => x.Header.Height).ToList();
        }

        public Task<bool> HaveSecondaryBlock(byte[] blockId)
        {
            var result = ForkChain.Exists(x => x.Entity.Header.BlockId == blockId);
            return Task.FromResult(result);
        }
    }
}