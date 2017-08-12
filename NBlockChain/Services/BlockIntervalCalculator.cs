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
        private readonly Lazy<long> _genesisTime;

        public BlockIntervalCalculator(INetworkParameters parameters, IDateTimeProvider dateTimeProvider, IBlockRepository blockRepository)
        {
            _parameters = parameters;
            _dateTimeProvider = dateTimeProvider;
            _genesisTime = new Lazy<long>(() => _blockRepository.GetGenesisBlockTime().Result);
        }

        public uint HeightNow => DetermineHeight(_dateTimeProvider.UtcTicks);

        public uint DetermineHeight(long now)
        {
            return Convert.ToUInt32(((now - _genesisTime.Value) / _parameters.BlockTime.Ticks) + 1);
        }

    }
}
