using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using NBlockchain.Interfaces;
using NBlockchain.Models;

namespace NBlockchain.Services
{
    public class NodeHost : INodeHost
    {        
        private readonly INetworkParameters _parameters;
        private readonly IBlockRepository _blockRepository;
        private readonly IBlockVerifier _blockVerifier;        
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ITransactionKeyResolver _transactionKeyResolver;        
        private readonly IPeerNetwork _peerNetwork;
        private readonly AutoResetEvent _blockEvent = new AutoResetEvent(true);        
        private readonly IPendingTransactionList _pendingTransactionList;
        private readonly IDifficultyCalculator _difficultyCalculator;

        private readonly Timer _pollTimer;
        

        public NodeHost(IBlockRepository blockRepository, IBlockVerifier blockVerifier, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, INetworkParameters parameters, IDateTimeProvider dateTimeProvider, ITransactionKeyResolver transactionKeyResolver, IPendingTransactionList pendingTransactionList, IPeerNetwork peerNetwork, IDifficultyCalculator difficultyCalculator)
        {
            _blockRepository = blockRepository;
            _blockVerifier = blockVerifier;        
            _serviceProvider = serviceProvider;
            _parameters = parameters;
            _dateTimeProvider = dateTimeProvider;
            _transactionKeyResolver = transactionKeyResolver;
            _pendingTransactionList = pendingTransactionList;
            _peerNetwork = peerNetwork;
            _difficultyCalculator = difficultyCalculator;
            _logger = loggerFactory.CreateLogger<NodeHost>();

            _peerNetwork.RegisterBlockReceiver(this);
            _peerNetwork.RegisterTransactionReceiver(this);
            
            _pollTimer = new Timer(GetMissingBlocks, null, TimeSpan.FromSeconds(5), _parameters.BlockTime);
        }
                
        public async Task<PeerDataResult> RecieveTail(Block block)
        {
            _blockEvent.WaitOne();
            try
            {
                _logger.LogDebug($"Recv tail {BitConverter.ToString(block.Header.BlockId)}");
                
                if (await _blockRepository.HaveBlock(block.Header.BlockId))
                    return PeerDataResult.Ignore;

                var prevHeader = await _blockRepository.GetNewestBlockHeader();

                if (prevHeader != null)
                {
                    if (!block.Header.PreviousBlock.SequenceEqual(prevHeader.BlockId))
                        return PeerDataResult.Ignore;

                    if (block.Header.Timestamp < prevHeader.Timestamp)
                        return PeerDataResult.Ignore;

                    var expectedDifficulty = await _difficultyCalculator.CalculateDifficulty(prevHeader.Timestamp);

                    if (block.Header.Difficulty < expectedDifficulty)
                        return PeerDataResult.Ignore;
                }
                else
                {
                    if (!(block.Header.PreviousBlock.SequenceEqual(Block.HeadKey) && await _blockRepository.IsEmpty()))
                        return PeerDataResult.Ignore;
                }

                if (!_blockVerifier.Verify(block))
                {
                    _logger.LogWarning($"Block verification failed for {BitConverter.ToString(block.Header.BlockId)}");
                    return PeerDataResult.Demerit;
                }

                var contentTxnIds = block.Transactions.Select(x => _transactionKeyResolver.ResolveKey(x)).ToList();

                //if (!_blockVerifier.VerifyContentThreshold(contentTxnIds, _pendingTransactionList.Get))
                //{
                //    _logger.LogWarning($"Block content verification failed for {BitConverter.ToString(block.Header.BlockId)}");
                //    return PeerDataResult.Ignore;
                //}
                                
                await _blockRepository.AddBlock(block);
                _pendingTransactionList.Remove(block.Transactions);

                _logger.LogDebug($"Accepted tail {BitConverter.ToString(block.Header.BlockId)}");                

                return PeerDataResult.Relay;
            }
            finally
            {
                _blockEvent.Set();
            }            
        }

        public async Task<PeerDataResult> RecieveBlock(Block block)
        {
            _blockEvent.WaitOne();
            try
            {
                _logger.LogDebug($"Recv block {BitConverter.ToString(block.Header.BlockId)}");

                if (await _blockRepository.HaveBlock(block.Header.BlockId))
                    return PeerDataResult.Ignore;

                if (!await _blockRepository.HaveBlock(block.Header.PreviousBlock))
                {
                    if (!(block.Header.PreviousBlock.SequenceEqual(Block.HeadKey) && await _blockRepository.IsEmpty()))
                        return PeerDataResult.Ignore;
                }

                if (!_blockVerifier.Verify(block))
                {
                    _logger.LogWarning($"Block verification failed for {BitConverter.ToString(block.Header.BlockId)}");
                    return PeerDataResult.Demerit;
                }

                await _blockRepository.AddBlock(block);

                _logger.LogDebug($"Accepted block {BitConverter.ToString(block.Header.BlockId)}");
            }
            finally
            {
                _blockEvent.Set();
            }

            GetMissingBlocks(null);
            return PeerDataResult.Ignore;
        }

        public Task<PeerDataResult> RecieveTransaction(TransactionEnvelope transaction)
        {            
            _logger.LogDebug($"Recv txn {transaction.OriginKey} from {transaction.Originator}");            
            var txnResult = _blockVerifier.VerifyTransaction(transaction, _pendingTransactionList.Get);
            
            if (txnResult == 0)
            {
                //var txnKey = _transactionKeyResolver.ResolveKey(transaction);
                if (_pendingTransactionList.Add(transaction))
                {
                    _logger.LogDebug($"Accepted txn {transaction.OriginKey} from {transaction.Originator}");
                    return Task.FromResult(PeerDataResult.Relay);
                }

                return Task.FromResult(PeerDataResult.Ignore);
            }
            _logger.LogDebug($"Rejected txn {transaction.OriginKey} from {transaction.Originator} code: {txnResult}");
            return Task.FromResult(PeerDataResult.Ignore);
        }
                

        public async Task SendTransaction(TransactionEnvelope transaction)
        {
            _logger.LogDebug("Sending txn");
            await RecieveTransaction(transaction);
            _peerNetwork.BroadcastTransaction(transaction);
        }
        
        private async void GetMissingBlocks(object state)
        {
            var prevHeader = await _blockRepository.GetNewestBlockHeader();

            if (prevHeader == null)
            {
                _logger.LogDebug("Requesting head block");
                _peerNetwork.RequestNextBlock(Block.HeadKey);
                return;
            }
            
            //if ((DateTime.UtcNow.Ticks - prevHeader.Timestamp) > _parameters.BlockTime.Ticks)
            {
                _logger.LogDebug($"Requesting missing block after {BitConverter.ToString(prevHeader.BlockId)}");
                _peerNetwork.RequestNextBlock(prevHeader.BlockId);
            }
        }
        
    }
}
