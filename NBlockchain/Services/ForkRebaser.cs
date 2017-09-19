using Microsoft.Extensions.Logging;
using NBlockchain.Interfaces;
using NBlockchain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBlockchain.Services
{
    public class ForkRebaser : IForkRebaser
    {
        private readonly IBlockRepository _blockRepository;        
        //private IReceiver _blockReceiver;
        private readonly ILogger _logger;

        public ForkRebaser(IBlockRepository blockRepository, ILoggerFactory loggerFactory)
        {            
            _blockRepository = blockRepository;
            _logger = loggerFactory.CreateLogger<ForkRebaser>();
        }

        public async Task<ICollection<Block>> RebaseChain(byte[] divergentId, byte[] targetTipId)
        {
            _logger.LogInformation($"Rebasing chain from {BitConverter.ToString(divergentId)} to {BitConverter.ToString(targetTipId)}");
            var currentTipHeader = await _blockRepository.GetBestBlockHeader();
            await _blockRepository.RewindChain(divergentId);
            var chainFork = await _blockRepository.GetFork(targetTipId);
            return chainFork.OrderBy(x => x.Header.Height).ToList();
        }


        public async Task<BlockHeader> FindKnownForkbase(byte[] forkTipId)
        {
            _logger.LogInformation($"Searching for fork base");
            var header = await _blockRepository.GetSecondaryHeader(forkTipId);

            var prevHeader = await _blockRepository.GetSecondaryHeader(header.PreviousBlock);
            while (prevHeader != null)
            {
                header = prevHeader;
                prevHeader = await _blockRepository.GetSecondaryHeader(prevHeader.PreviousBlock);
            }
            return header;
        }
        
    }
}
