using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using NBlockChain.Interfaces;
using NBlockChain.Models;
using System.Collections.Concurrent;

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
        private readonly IBuildQueue _buildQueue;
        private readonly IPeerNetwork _peerNetwork;
        private readonly AutoResetEvent _tailEvent = new AutoResetEvent(true);
        private readonly TransactionBucket _txnBucket = new TransactionBucket();
        

        private readonly byte[] HeadKey = new byte[] { 0x0 };
        private readonly Timer _blockTimer;
        private readonly Timer _pollTimer;
        


        public NodeHost(IBlockRepository blockRepository, IBlockVerifier blockVerifier, ILoggerFactory loggerFactory, IBlockBuilder blockBuilder, IServiceProvider serviceProvider, INetworkParameters parameters, IDateTimeProvider dateTimeProvider, ITransactionKeyResolver transactionKeyResolver, IBlockIntervalCalculator intervalCalculator, IBuildQueue buildQueue, IPeerNetwork peerNetwork)
        {
            _blockRepository = blockRepository;
            _blockVerifier = blockVerifier;
            _blockBuilder = blockBuilder;
            _serviceProvider = serviceProvider;
            _parameters = parameters;
            _dateTimeProvider = dateTimeProvider;
            _transactionKeyResolver = transactionKeyResolver;
            _intervalCalculator = intervalCalculator;
            _buildQueue = buildQueue;
            _peerNetwork = peerNetwork;
            _logger = loggerFactory.CreateLogger<NodeHost>();

            _peerNetwork.RegisterBlockReceiver(this);
            _peerNetwork.RegisterTransactionReceiver(this);
            _blockTimer = new Timer(RollOver, null, _intervalCalculator.TimeUntilNextBlock, _parameters.BlockTime);
            _pollTimer = new Timer(GetMissingBlocks, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30));
        }

        public void StartBuildingBlocks(KeyPair builderKeys)
        {
            _blockBuilder.FlushQueue();
            _buildQueue.Start(builderKeys);
        }

        public void StopBuildingBlocks()
        {
            _buildQueue.Stop();
            _blockBuilder.FlushQueue();
        }

        public async Task RecieveTail(Block block)
        {
            _tailEvent.WaitOne();
            try
            {
                _logger.LogDebug($"Recv tail {BitConverter.ToString(block.Header.BlockId)}");

                if (block.Header.Difficulty != _parameters.Difficulty)
                    return;

                if (await _blockRepository.HaveBlock(block.Header.BlockId))
                    return;

                var prevHeader = await _blockRepository.GetNewestBlockHeader();

                if (prevHeader != null)
                {
                    if (!block.Header.PreviousBlock.SequenceEqual(prevHeader.BlockId))
                        return;
                }
                else
                {
                    if (!(block.Header.PreviousBlock.SequenceEqual(HeadKey) && await _blockRepository.IsEmpty()))
                        return;
                }

                if (!_blockVerifier.Verify(block, _parameters.Difficulty))
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

                _logger.LogDebug($"Accepted tail {BitConverter.ToString(block.Header.BlockId)}");

                _buildQueue.CancelBlock(block.Header.Height);
            }
            finally
            {
                _tailEvent.Set();
            }            
        }

        public async Task<bool> RecieveBlock(Block block)
        {
            _logger.LogDebug($"Recv block {BitConverter.ToString(block.Header.BlockId)}");

            if (await _blockRepository.HaveBlock(block.Header.BlockId))
                return true;

            if (!await _blockRepository.HaveBlock(block.Header.PreviousBlock))
            {
                if (!(block.Header.PreviousBlock.SequenceEqual(HeadKey) && await _blockRepository.IsEmpty()))
                    return true;
            }

            if (!_blockVerifier.Verify(block, block.Header.Difficulty))
            {
                _logger.LogWarning($"Block verification failed for {BitConverter.ToString(block.Header.BlockId)}");
                return false;
            }

            await _blockRepository.AddBlock(block);

            _logger.LogDebug($"Accepted block {BitConverter.ToString(block.Header.BlockId)}");

            GetMissingBlocks(null);
            return true;
        }

        public async Task RecieveTransaction(TransactionEnvelope transaction)
        {            
            _logger.LogDebug($"Recv txn {transaction.OriginKey} from {transaction.Originator}");
            var txnResult = _blockVerifier.VerifyTransaction(transaction);
            
            if (txnResult == 0)
            {
                var txnKey = _transactionKeyResolver.ResolveKey(transaction);
                var accepted = _txnBucket.AddTransaction(txnKey, _intervalCalculator.HeightNow);

                if (_buildQueue.Running && accepted)
                {
                    _logger.LogDebug($"Accepted txn {transaction.OriginKey} from {transaction.Originator}");
                    _blockBuilder.QueueTransaction(transaction);                    
                }
            }
            else
            {
                _logger.LogDebug($"Rejected txn {transaction.OriginKey} from {transaction.Originator} code: {txnResult}");
                //broadcast rejection???
            }

            await Task.Yield();
        }

        public async Task BuildGenesisBlock(KeyPair builderKeys)
        {
            _logger.LogInformation($"Building genesis block");
            var cts = new CancellationTokenSource();
            var block = await _blockBuilder.BuildBlock(HeadKey, 0, builderKeys, cts.Token);
            await RecieveTail(block);
            await _peerNetwork.BroadcastBlock(block);
            _intervalCalculator.ResetGenesisTime();
            _blockTimer.Change(_intervalCalculator.TimeUntilNextBlock, _parameters.BlockTime);
            _logger.LogInformation($"Built genesis block {BitConverter.ToString(block.Header.BlockId)}");
        }

        public async Task SendTransaction(TransactionEnvelope transaction)
        {
            await RecieveTransaction(transaction);
            await _peerNetwork.BroadcastTransaction(transaction);
        }

        private async void RollOver(object state)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            _blockTimer.Change(_intervalCalculator.TimeUntilNextBlock, _parameters.BlockTime);
            var height = _intervalCalculator.HeightNow;
            _buildQueue.EnqueueBlock(height);
        }

        private async void GetMissingBlocks(object state)
        {
            var prevHeader = await _blockRepository.GetNewestBlockHeader();

            if (prevHeader == null)
            {
                await _peerNetwork.RequestNextBlock(HeadKey);
                return;
            }

            if (prevHeader.Height < (_intervalCalculator.HeightNow - 1))
            {
                await _peerNetwork.RequestNextBlock(prevHeader.BlockId);
            }
        }
        
    }
}
