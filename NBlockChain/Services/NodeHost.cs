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
        private readonly ITransactionKeyResolver _transactionKeyResolver;
        private readonly IBlockIntervalCalculator _intervalCalculator;
        private readonly AutoResetEvent _tailEvent = new AutoResetEvent(true);
        private readonly TransactionBucket _txnBucket = new TransactionBucket();

        private bool _isBuilder = false;
        private KeyPair _builderKeys;
        private Timer _blockTimer;
        private CancellationTokenSource _cancelTokenSource;

        private IPeerNetwork PeerNetwork => _serviceProvider.GetService<IPeerNetwork>();


        public NodeHost(IBlockRepository blockRepository, IBlockVerifier blockVerifier, ILoggerFactory loggerFactory, IBlockBuilder blockBuilder, IServiceProvider serviceProvider, INetworkParameters parameters, IDateTimeProvider dateTimeProvider, ITransactionKeyResolver transactionKeyResolver, IBlockIntervalCalculator intervalCalculator)
        {
            _blockRepository = blockRepository;
            _blockVerifier = blockVerifier;
            _blockBuilder = blockBuilder;
            _serviceProvider = serviceProvider;
            _parameters = parameters;
            _dateTimeProvider = dateTimeProvider;
            _transactionKeyResolver = transactionKeyResolver;
            _intervalCalculator = intervalCalculator;
            _logger = loggerFactory.CreateLogger<NodeHost>();
                        
            _blockTimer = new Timer(RollOver, null, _intervalCalculator.TimeUntilNextBlock, _parameters.BlockTime);
        }

        public void StartBuildingBlocks(KeyPair builderKeys)
        {
            _builderKeys = builderKeys;
            _blockBuilder.FlushQueue();
            _isBuilder = true;
        }

        public void StopBuildingBlocks()
        {
            _isBuilder = false;
            _blockBuilder.FlushQueue();
        }

        public async Task RecieveTail(Block block)
        {
            _tailEvent.WaitOne();
            try
            {
                if (await _blockRepository.HaveBlock(block.Header.BlockId))
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

                var contentTxnIds = block.Transactions.Select(x => _transactionKeyResolver.ResolveKey(x)).ToList();

                if (!_blockVerifier.VerifyContentThreshold(contentTxnIds, _txnBucket.GetBucket(block.Header.Height)))
                {
                    _logger.LogWarning($"Block content verification failed for {BitConverter.ToString(block.Header.BlockId)}");
                    return;
                }

                _txnBucket.Shift(block.Header.Height, contentTxnIds);

                await _blockRepository.AddBlock(block);

                if (_cancelTokenSource != null)
                    _cancelTokenSource.Cancel();
            }
            finally
            {
                _tailEvent.Set();
            }            
        }

        public async Task RecieveBlock(Block block)
        {
            if (await _blockRepository.HaveBlock(block.Header.BlockId))
                return;

            if (!await _blockRepository.HaveBlock(block.Header.PreviousBlock))
                return;

            if (!_blockVerifier.Verify(block))
            {
                _logger.LogWarning($"Block verification failed for {BitConverter.ToString(block.Header.BlockId)}");
                return;
            }

            await _blockRepository.AddBlock(block);
        }

        public async Task RecieveTransaction(TransactionEnvelope transaction)
        {
            var txnResult = _blockVerifier.VerifyTransaction(transaction);
            
            if (txnResult == 0)
            {
                var txnKey = _transactionKeyResolver.ResolveKey(transaction);
                _txnBucket.AddTransaction(txnKey, _intervalCalculator.HeightNow);
            }

            if (_isBuilder)
            {
                if (txnResult == 0)
                {
                    await _blockBuilder.QueueTransaction(transaction);
                }
                else
                {
                    //TODO: broadcast rejections
                }
            }
        }

        private async void RollOver(object state)
        {
            if (_isBuilder)
            {
                _cancelTokenSource = new CancellationTokenSource();
                
                var prevBlockHeader = await _blockRepository.GetNewestBlockHeader();                

                var block = await _blockBuilder.BuildBlock(prevBlockHeader.BlockId, _intervalCalculator.HeightNow, _builderKeys, _cancelTokenSource.Token);

                if (block != null)
                {
                    if (block.Header.Status == BlockStatus.Notarized)
                    {
                        var task1 = RecieveTail(block);
                        var task2 = PeerNetwork.BroadcastBlock(block);
                    }
                }
            }
            await Task.Delay(TimeSpan.FromSeconds(1));
            _blockTimer.Change(_intervalCalculator.TimeUntilNextBlock, _parameters.BlockTime);
        }
    }
}
