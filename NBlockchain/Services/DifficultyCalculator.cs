using NBlockchain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NBlockchain.Services
{
    public class DifficultyCalculator : IDifficultyCalculator
    {
        private readonly IBlockRepository _blockRepository;
        private readonly INetworkParameters _parameters;
        private readonly TimeSpan _sampleInterval = TimeSpan.FromHours(1);
        private readonly uint _step = 1;
        private readonly uint _genesisValue = 700;

        public DifficultyCalculator(IBlockRepository blockRepository, INetworkParameters parameters)
        {
            _blockRepository = blockRepository;
            _parameters = parameters;
        }

        public async Task<uint> CalculateDifficulty(long timestamp)
        {
            var end = new DateTime(timestamp + 1);
            var start = end.Subtract(_sampleInterval);
            var latestHeader = await _blockRepository.GetBestBlockHeader();

            if (latestHeader == null)
                return _genesisValue;

            var avg = await _blockRepository.GetAverageBlockTimeInSecs(start, end);
            var avgBlockTime = TimeSpan.FromSeconds(avg);

            if (_parameters.BlockTime > avgBlockTime)
                return latestHeader.Difficulty + _step;

            if (_parameters.BlockTime < avgBlockTime)
                return latestHeader.Difficulty - _step;

            return latestHeader.Difficulty;
        }
    }
}