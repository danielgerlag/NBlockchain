using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using NBlockChain.Interfaces;
using NBlockChain.Models;

namespace NBlockChain.Services
{
    public class NodeHost : INodeHost
    {
        private readonly INetworkParameters _parameters;
        private readonly IBlockRepository _blockRepository;
        private readonly IBlockVerifier _blockVerifier;
        private readonly IBlockBuilder _blockBuilder;
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDateTimeProvider _dateTimeProvider;

        private bool _isBuilder = false;
        private KeyPair _builderKeys;
        private Timer _blockTimer;

        private IPeerNetwork PeerNetwork => _serviceProvider.GetService<IPeerNetwork>();


        public NodeHost(IBlockRepository blockRepository, IBlockVerifier blockVerifier, ILoggerFactory loggerFactory, IBlockBuilder blockBuilder, IServiceProvider serviceProvider, INetworkParameters parameters, IDateTimeProvider dateTimeProvider)
        {
            _blockRepository = blockRepository;
            _blockVerifier = blockVerifier;
            _blockBuilder = blockBuilder;
            _serviceProvider = serviceProvider;
            _parameters = parameters;
            _dateTimeProvider = dateTimeProvider;
            _logger = loggerFactory.CreateLogger<NodeHost>();
        }

        public void StartBuildingBlocks(KeyPair builderKeys)
        {
            _builderKeys = builderKeys;
            _blockBuilder.FlushQueue();
            _isBuilder = true;
            var prevBlockHeader = _blockRepository.GetNewestBlockHeader().Result;
            _blockTimer = new Timer(BuildBlock, null, TimeSpan.FromTicks((_parameters.BlockTime.Ticks + prevBlockHeader.Timestamp) - _dateTimeProvider.UtcTicks), _parameters.BlockTime);
        }

        public void StopBuildingBlocks()
        {
            _blockTimer.Dispose();
            _isBuilder = false;
            _blockBuilder.FlushQueue();
        }

        public async Task RecieveBlock(Block block)
        {
            if (await _blockRepository.HaveBlock(block))
                return;

            var prevHeader = await _blockRepository.GetNewestBlockHeader();

            if (!block.Header.PreviousBlock.SequenceEqual(prevHeader.BlockId))
            {
                return;
            }

            if (!_blockVerifier.Verify(block))
            {
                _logger.LogWarning($"Block verification failed for {BitConverter.ToString(block.Header.BlockId)}");
                return;
            }

            await _blockRepository.AddBlock(block);
        }

        public async Task RecieveTransaction(TransactionEnvelope transaction)
        {
            if (_isBuilder)
            {
                var result = await _blockBuilder.QueueTransaction(transaction);
                //TODO: broadcast rejections
            }
        }

        private async void BuildBlock(object state)
        {
            var prevBlockHeader = await _blockRepository.GetNewestBlockHeader();
            
            if ((_parameters.BlockTime.Ticks + prevBlockHeader.Timestamp) > _dateTimeProvider.UtcTicks)
            {
                _blockTimer.Change(TimeSpan.FromTicks((_parameters.BlockTime.Ticks + prevBlockHeader.Timestamp) - _dateTimeProvider.UtcTicks), _parameters.BlockTime);
                return;
            }

            var block = await _blockBuilder.BuildBlock(prevBlockHeader.BlockId, _builderKeys);

            if (block.Header.Status == BlockStatus.Notarized)
                await PeerNetwork.BroadcastBlock(block);
        }
    }
}
