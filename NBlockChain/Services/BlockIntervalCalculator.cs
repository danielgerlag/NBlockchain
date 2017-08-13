using NBlockChain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NBlockChain.Services
{
    public class BlockIntervalCalculator : IBlockIntervalCalculator
    {
        private readonly INetworkParameters _parameters;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IBlockRepository _blockRepository;
        private Lazy<long> _genesisTime;

        public BlockIntervalCalculator(INetworkParameters parameters, IDateTimeProvider dateTimeProvider, IBlockRepository blockRepository)
        {
            _parameters = parameters;
            _dateTimeProvider = dateTimeProvider;
            _blockRepository = blockRepository;
            ResetGenesisTime();
        }

        public void ResetGenesisTime() => _genesisTime = new Lazy<long>(() => _blockRepository.GetGenesisBlockTime().Result);
        
        public uint DetermineHeight(long now) => Convert.ToUInt32(((now - _genesisTime.Value) / _parameters.BlockTime.Ticks) + 1);

        public uint HeightNow => DetermineHeight(_dateTimeProvider.UtcTicks);

        public long NextBlockTime => (_genesisTime.Value + (HeightNow * _parameters.BlockTime.Ticks));

        public long LastBlockTime => (_genesisTime.Value + ((HeightNow - 1) * _parameters.BlockTime.Ticks));

        public TimeSpan TimeUntilNextBlock => new TimeSpan(NextBlockTime - _dateTimeProvider.UtcTicks);
    }
}
