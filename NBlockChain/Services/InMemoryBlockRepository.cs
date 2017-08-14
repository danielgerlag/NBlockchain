using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBlockChain.Interfaces;
using NBlockChain.Models;

namespace NBlockChain.Services
{
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

        public async Task<long> GetGenesisBlockTime()
        {
            _resetEvent.WaitOne();
            try
            {
                if (!_blocks.Any())
                    return DateTime.UtcNow.Ticks;

                return await Task.FromResult(_blocks.Min(x => x.Header.Timestamp));
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
    }
}
