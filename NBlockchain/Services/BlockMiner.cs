using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using NBlockchain.Interfaces;
using NBlockchain.Models;

namespace NBlockchain.Services
{
    public class BlockMiner : IBlockMiner
    {
        private readonly INetworkParameters _networkParameters;
        private readonly ITransactionKeyResolver _transactionKeyResolver;
        private readonly IMerkleTreeBuilder _merkleTreeBuilder;        
        private readonly IBlockbaseTransactionBuilder _blockbaseBuilder;
        private readonly IConsensusMethod _consensusMethod;
        private readonly ILogger _logger;
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(true);
        private readonly IUnconfirmedTransactionPool _unconfirmedTransactionPool;
        private readonly IBlockRepository _blockRepository;
        private readonly IPeerNetwork _peerNetwork;
        private readonly IReceiver _blockReciever;
        private readonly IDifficultyCalculator _difficultyCalculator;

        private KeyPair _builderKeys;
        private Task _buildTask;
        private bool _buildGenesis = false;

        private CancellationTokenSource _buildCancelToken;
        private CancellationTokenSource _blockCancelToken;
                

        public BlockMiner(ITransactionKeyResolver transactionKeyResolver, IMerkleTreeBuilder merkleTreeBuilder, INetworkParameters networkParameters, IBlockbaseTransactionBuilder blockbaseBuilder, IPeerNetwork peerNetwork, IConsensusMethod consensusMethod, IUnconfirmedTransactionPool unconfirmedTransactionPool, IBlockRepository blockRepository, IReceiver blockReciever, IDifficultyCalculator difficultyCalculator, ILoggerFactory loggerFactory)
        {
            _networkParameters = networkParameters;
            _peerNetwork= peerNetwork;
            _blockbaseBuilder = blockbaseBuilder;
            _blockReciever = blockReciever;
            _consensusMethod = consensusMethod;
            _difficultyCalculator = difficultyCalculator;
            _transactionKeyResolver = transactionKeyResolver;
            _merkleTreeBuilder = merkleTreeBuilder;
            _logger = loggerFactory.CreateLogger<BlockMiner>();
            _blockRepository = blockRepository;
            _unconfirmedTransactionPool = unconfirmedTransactionPool;
            _unconfirmedTransactionPool.Changed += UnconfirmedTransactionPoolChanged;
        }


        public void Start(KeyPair builderKeys, bool genesis)
        {
            _builderKeys = builderKeys;
            _buildGenesis = genesis;
            _buildCancelToken = new CancellationTokenSource();
            _buildTask = Task.Factory.StartNew(Mine);
        }

        public void Stop()
        {
            _buildCancelToken.Cancel();
        }
                
        private async void Mine()
        {
            while (!_buildCancelToken.IsCancellationRequested)
            {
                _blockCancelToken = new CancellationTokenSource();
                var prevHeader = await _blockRepository.GetBestBlockHeader();
                if (prevHeader == null)
                {
                    if (!_buildGenesis)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        continue;
                    }
                    prevHeader = new BlockHeader() { BlockId = Block.HeadKey, Height = 0, Timestamp = DateTime.UtcNow.Ticks };
                    _logger.LogInformation($"Mining genesis block");
                }
                var difficulty = await _difficultyCalculator.CalculateDifficulty(prevHeader.Timestamp);
                var block = await AssembleBlock(prevHeader.BlockId, prevHeader.Height + 1, difficulty, _blockCancelToken.Token);
                if (block != null)
                {
                    if (block.Header.Status == BlockStatus.Confirmed)
                    {
                        var recvResult = await _blockReciever.RecieveBlock(block);
                        if (recvResult == PeerDataResult.Relay)
                            _peerNetwork.BroadcastTail(block);
                    }
                }
            }
        }

        private void UnconfirmedTransactionPoolChanged(object sender, EventArgs e)
        {
            _resetEvent.WaitOne();
            try
            {
                if (_blockCancelToken != null)
                    _blockCancelToken.Cancel();

            }
            finally
            {
                _resetEvent.Set();
            }

        }

        private async Task<Block> AssembleBlock(byte[] prevBlock, uint height, uint difficulty, CancellationToken cancellationToken)
        {
            var targetTxns = _unconfirmedTransactionPool.Get;
            targetTxns.Add(await _blockbaseBuilder.Build(_builderKeys, targetTxns));
            var merkleRoot = await _merkleTreeBuilder.BuildTree(targetTxns.Select(x => x.TransactionId).ToList());

            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug($"Cancelled building block {height}");
                return null;
            }

            var result = new Block()
            {
                Transactions = targetTxns,
                MerkleRootNode = merkleRoot,
                Header = new BlockHeader()
                {
                    MerkelRoot = merkleRoot.Value,                    
                    Status = BlockStatus.Unconfirmed,
                    Version = _networkParameters.HeaderVersion,
                    Height = height,
                    PreviousBlock = prevBlock,
                    Difficulty = difficulty
                }
            };

            _logger.LogDebug($"Building consensus for block {height}");
            await _consensusMethod.BuildConsensus(result, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug($"Cancelled building block {height}");
                return null;
            }

            _logger.LogDebug($"Built block {height} - {BitConverter.ToString(result.Header.BlockId)}");

            return result;
        }
    }
}
