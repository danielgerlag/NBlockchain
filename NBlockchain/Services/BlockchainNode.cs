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
    public class BlockchainNode : IBlockchainNode
    {        
        private readonly INetworkParameters _parameters;
        private readonly IBlockRepository _blockRepository;
        private readonly IBlockVerifier _blockVerifier;        
        private readonly ILogger _logger;        
        private readonly IForkRebaser _forkRebaser;
        private readonly IReceiver _receiver;        
        private readonly IPeerNetwork _peerNetwork;
        private readonly AutoResetEvent _blockEvent = new AutoResetEvent(true);
        private readonly IUnconfirmedTransactionPool _unconfirmedTransactionPool;
        private readonly IDifficultyCalculator _difficultyCalculator;

        public readonly Timer PollTimer;
        

        public BlockchainNode(IBlockRepository blockRepository, IBlockVerifier blockVerifier, IReceiver receiver, ILoggerFactory loggerFactory, IForkRebaser forkRebaser, INetworkParameters parameters, IUnconfirmedTransactionPool unconfirmedTransactionPool, IPeerNetwork peerNetwork, IDifficultyCalculator difficultyCalculator)
        {
            _blockRepository = blockRepository;
            _blockVerifier = blockVerifier;
            _receiver = receiver;
            _parameters = parameters;
            _forkRebaser = forkRebaser;
            _unconfirmedTransactionPool = unconfirmedTransactionPool;
            _peerNetwork = peerNetwork;
            _difficultyCalculator = difficultyCalculator;
            //_expectedBlockList = expectedBlockList;
            _logger = loggerFactory.CreateLogger<BlockchainNode>();

            _receiver.OnReceiveBlock += RecieveBlock;
            _receiver.OnRecieveTransaction += RecieveTransaction;

            PollTimer = new Timer(GetMissingBlocks, null, TimeSpan.FromSeconds(5), _parameters.BlockTime);
        }
                
        public async Task<PeerDataResult> RecieveBlock(Block block)
        {
            var result = PeerDataResult.Ignore;
            if (!_blockEvent.WaitOne(TimeSpan.FromSeconds(30)))
            {
                _logger.LogError($"Timeout waiting for block lock event");
                return PeerDataResult.Ignore;
            }
            try
            {
                result = await RecieveBlockUnprotected(block);
            }
            finally
            {
                _blockEvent.Set();
            }

            if (result != PeerDataResult.Demerit)
            {
                GetMissingBlocks(null);
            }

            return result;
        }

        private async Task<PeerDataResult> RecieveBlockUnprotected(Block block)
        {
            var isTip = false;
            
            _logger.LogInformation($"Recv block {block.Header.Height} {BitConverter.ToString(block.Header.BlockId)}");

            if (await _blockRepository.HavePrimaryBlock(block.Header.BlockId))
            {
                _logger.LogInformation("already have block");
                return PeerDataResult.Ignore;
            }

            if (!await _blockVerifier.Verify(block))
            {
                _logger.LogWarning($"Block verification failed for {BitConverter.ToString(block.Header.BlockId)}");
                return PeerDataResult.Demerit;
            }

            if (!await _blockVerifier.VerifyBlockRules(block, true))
            {
                _logger.LogWarning($"Block rules failed for {BitConverter.ToString(block.Header.BlockId)}");
                return PeerDataResult.Demerit;
            }

            var prevHeader = await _blockRepository.GetBlockHeader(block.Header.PreviousBlock);
            var bestHeader = await _blockRepository.GetBestBlockHeader();
            var isEmpty = await _blockRepository.IsEmpty();
            bool mainChain = false;
            bool rebaseChain = false;
            isTip = (block.Header.PreviousBlock.SequenceEqual(bestHeader?.BlockId ?? Block.HeadKey));
            _logger.LogInformation($"Is Tip {isTip}");

            if (prevHeader != null)
            {
                _logger.LogInformation("Do Have previous block");

                if (block.Header.Timestamp < prevHeader.Timestamp)
                {
                    _logger.LogInformation("Timestamps dont match");
                    return PeerDataResult.Ignore;
                }

                if (block.Header.Height != (prevHeader.Height + 1))
                {
                    _logger.LogInformation($"Height mismatch prev: {prevHeader.Height}, this: {block.Header.Height}");
                    return PeerDataResult.Ignore;
                }

                if (!isTip)
                {
                    var prevMain = await _blockRepository.GetPrimaryHeader(block.Header.Height - 1);
                    var isPrevOnMainChain = prevMain?.BlockId.SequenceEqual(block.Header.PreviousBlock) ?? false;
                    var mainExisiting = await _blockRepository.GetPrimaryHeader(block.Header.Height);
                    mainChain = ((mainExisiting == null) && (isPrevOnMainChain));
                }
                else
                {
                    mainChain = true;
                }

                rebaseChain = ((block.Header.Height > bestHeader.Height) && !mainChain);

                _logger.LogInformation($"Processing block, have prev, main chain: {mainChain}, rebase: {rebaseChain}");

                var expectedDifficulty = await _difficultyCalculator.CalculateDifficulty(prevHeader.Timestamp);
                if ((mainChain) && (block.Header.Difficulty < expectedDifficulty))
                {
                    _logger.LogInformation("Difficulty mismatch");
                    return PeerDataResult.Ignore;
                }
            }
            else
            {
                _logger.LogInformation("Dont have prev block");
                if (isEmpty && !block.Header.PreviousBlock.SequenceEqual(Block.HeadKey))
                {
                    _logger.LogInformation("not first block but am empty");
                    return PeerDataResult.Ignore;
                }
                mainChain = isEmpty;
                _logger.LogInformation($"Processing block, missing prev, main chain: {mainChain}, rebase: {rebaseChain}");
                _peerNetwork.RequestBlock(block.Header.PreviousBlock);
            }

            if (mainChain)
            {
                _logger.LogInformation("processing for main chain");
                if (!await _blockVerifier.VerifyTransactions(block))
                {
                    _logger.LogWarning($"Block txn verification failed for {BitConverter.ToString(block.Header.BlockId)}");
                    return PeerDataResult.Demerit;
                }

                await _blockRepository.AddBlock(block);
                _unconfirmedTransactionPool.Remove(block.Transactions);
            }
            else
            {
                if (!await _blockRepository.HaveSecondaryBlock(block.Header.BlockId))
                {
                    _logger.LogInformation($"Adding detached block");
                    await _blockRepository.AddSecondaryBlock(block);
                }
                if (rebaseChain)
                {
                    _logger.LogInformation($"Searching for divergent block");
                    var divergentHeader = await _blockRepository.GetDivergentHeader(block.Header.BlockId);
                    if (divergentHeader != null)
                    {
                        _logger.LogInformation($"Rebasing chain from {divergentHeader.Height}");
                        var replayBlocks = await _forkRebaser.RebaseChain(divergentHeader.BlockId, block.Header.BlockId);
                        _logger.LogInformation($"Replaying {replayBlocks.Count} blocks");
                        foreach (var replayBlock in replayBlocks)
                        {
                            var replayResult = await RecieveBlockUnprotected(replayBlock);
                            if (replayResult == PeerDataResult.Demerit)
                                break;
                        }
                        _logger.LogInformation($"Replay complete");
                    }
                    else
                    {
                        _logger.LogInformation($"Divergent block not found");
                        var firstForkHeader = await _forkRebaser.FindKnownForkbase(block.Header.BlockId);
                        if (firstForkHeader != null)
                            _peerNetwork.RequestBlock(firstForkHeader.PreviousBlock);
                    }
                }
            }
            
            if (isTip)
            {
                _logger.LogDebug($"Accepted tip block {BitConverter.ToString(block.Header.BlockId)}");
                return PeerDataResult.Relay;
            }
            else
            {
                _logger.LogDebug($"Accepted block {BitConverter.ToString(block.Header.BlockId)}");
                return PeerDataResult.Ignore;
            }
        }

        public async Task<PeerDataResult> RecieveTransaction(Transaction transaction)
        {            
            _logger.LogDebug($"Recv txn {BitConverter.ToString(transaction.TransactionId)}");
            var txnResult = await _blockVerifier.VerifyTransaction(transaction, _unconfirmedTransactionPool.Get);
            
            if (txnResult == 0)
            {
                if (_unconfirmedTransactionPool.Add(transaction))
                {
                    _logger.LogDebug($"Accepted txn {BitConverter.ToString(transaction.TransactionId)}");
                    return PeerDataResult.Relay;
                }

                return PeerDataResult.Ignore;
            }
            _logger.LogDebug($"Rejected txn {BitConverter.ToString(transaction.TransactionId)} code: {txnResult}");
            return PeerDataResult.Ignore;
        }
                

        public async Task SendTransaction(Transaction transaction)
        {
            _logger.LogDebug("Sending txn");
            if (await _receiver.RecieveTransaction(transaction) == PeerDataResult.Relay)
                _peerNetwork.BroadcastTransaction(transaction);
        }
        
        private async void GetMissingBlocks(object state)
        {
            _logger.LogInformation("GetMissingBlocks");

            var bestHeader = await _blockRepository.GetBestBlockHeader();

            if (bestHeader == null)
            {
                _logger.LogInformation("Requesting head block");
                //_expectedBlockList.ExpectNext(Block.HeadKey);
                _peerNetwork.RequestNextBlock(Block.HeadKey);
                return;
            }
            
            //if ((DateTime.UtcNow.Ticks - prevHeader.Timestamp) > _parameters.BlockTime.Ticks)
            {
                _logger.LogInformation($"Requesting missing block after {BitConverter.ToString(bestHeader.BlockId)}");
                //_expectedBlockList.ExpectNext(prevHeader.BlockId);
                var cached = await _blockRepository.GetNextBlock(bestHeader.BlockId);
                if (cached == null)
                {
                    //_peerNetwork.RequestNextBlock(bestHeader.BlockId);
                    _peerNetwork.RequestBlockByHeight(bestHeader.Height + 1);
                }
                else
                {
                    _logger.LogInformation("Have cached block");
                    if (await _receiver.RecieveBlock(cached) == PeerDataResult.Demerit)
                    {
                        await _blockRepository.DiscardSecondaryBlock(cached.Header.BlockId);
                    }
                }                
            }
        }
                
    }
}
