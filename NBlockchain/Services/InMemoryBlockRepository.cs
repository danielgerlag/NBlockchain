using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBlockchain.Interfaces;
using NBlockchain.Models;

namespace NBlockchain.Services
{
    /// <summary>
    /// In-memory block repository for testing & demo purposes
    /// </summary>
    public class InMemoryBlockRepository : IBlockRepository
    {
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(true);
        private readonly ICollection<Block> _blocks = new HashSet<Block>();

        public InMemoryBlockRepository()
        {
        }

        public async Task AddBlock(Block block)
        {
            _resetEvent.WaitOne();
            try
            {
                _blocks.Add(block);
                await Task.Yield();
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public async Task<bool> HaveBlock(byte[] blockId)
        {
            _resetEvent.WaitOne();
            try
            {
                return await Task.FromResult(_blocks.Any(x => x.Header.BlockId.SequenceEqual(blockId)));
                
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public async Task<BlockHeader> GetNewestBlockHeader()
        {
            _resetEvent.WaitOne();
            try
            {
                if (!_blocks.Any())
                    return null;

                var max = _blocks.Max(x => x.Header.Height);
                var block = _blocks.FirstOrDefault(x => x.Header.Height == max);
                return await Task.FromResult(block?.Header);
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public async Task<Block> GetNextBlock(byte[] prevBlockId)
        {
            _resetEvent.WaitOne();
            try
            {
                var block = _blocks.FirstOrDefault(x => x.Header.PreviousBlock.SequenceEqual(prevBlockId));
                return await Task.FromResult(block);
            }
            finally
            {
                _resetEvent.Set();
            }
        }               

        public async Task<bool> IsEmpty()
        {
            _resetEvent.WaitOne();
            try
            {
                return await Task.FromResult(!_blocks.Any());
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public async Task<int> GetAverageBlockTimeInSecs(DateTime startUtc, DateTime endUtc)
        {
            _resetEvent.WaitOne();
            try
            {
                var startTicks = startUtc.Ticks;
                var endTicks = endUtc.Ticks;
                var sample = _blocks.Where(x => x.Header.Timestamp > startTicks && x.Header.Timestamp < endTicks && x.Header.Height > 1);
                if (sample.Count() == 0)
                    return 0;

                var avg = sample.Average(x => (x.Header.Timestamp - (_blocks.First(y => y.Header.BlockId.SequenceEqual(x.Header.PreviousBlock)).Header.Timestamp)));
                
                return Convert.ToInt32(TimeSpan.FromTicks(Convert.ToInt64(avg)).TotalSeconds);
            }
            finally
            {
                _resetEvent.Set();
            }
        }
    }
}
