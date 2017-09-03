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
    public class BlockBuilder : IBlockBuilder
    {
        private readonly INetworkParameters _networkParameters;
        private readonly ITransactionKeyResolver _transactionKeyResolver;
        private readonly IMerkleTreeBuilder _merkleTreeBuilder;        
        private readonly IBlockbaseTransactionBuilder _blockbaseBuilder;
        private readonly IBlockNotary _blockNotary;
        private readonly ILogger _logger;
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(true);
        private readonly IPendingTransactionList _pendingTransactionList;
        private readonly IBlockRepository _blockRepository;
        private readonly IPeerNetwork _peerNetwork;
        private readonly IBlockReceiver _blockReciever;

        private KeyPair _builderKeys;
        private Task _buildTask;
        private bool _buildGenesis = false;

        private CancellationTokenSource _buildCancelToken;
        private CancellationTokenSource _blockCancelToken;

        private readonly byte[] HeadKey = new byte[] { 0x0 };

        public BlockBuilder(ITransactionKeyResolver transactionKeyResolver, IMerkleTreeBuilder merkleTreeBuilder, INetworkParameters networkParameters, IBlockbaseTransactionBuilder blockbaseBuilder, IPeerNetwork peerNetwork, IBlockNotary blockNotary, IPendingTransactionList pendingTransactionList, IBlockRepository blockRepository, IBlockReceiver blockReciever, ILoggerFactory loggerFactory)
        {
            _networkParameters = networkParameters;
            _peerNetwork= peerNetwork;
            _blockbaseBuilder = blockbaseBuilder;
            _blockReciever = blockReciever;
            _blockNotary = blockNotary;
            
            _transactionKeyResolver = transactionKeyResolver;
            _merkleTreeBuilder = merkleTreeBuilder;
            _logger = loggerFactory.CreateLogger<BlockBuilder>();
            _blockRepository = blockRepository;
            _pendingTransactionList = pendingTransactionList;
            _pendingTransactionList.Changed += PendingTransactionList_Changed;
        }


        public void Start(KeyPair builderKeys, bool genesis)
        {
            _builderKeys = builderKeys;
            _buildGenesis = genesis;
            _buildTask = Task.Factory.StartNew(BuildTask);
        }

        public void Stop()
        {
            _buildCancelToken.Cancel();
        }
                
        private async void BuildTask()
        {
            while (!_buildCancelToken.IsCancellationRequested)
            {
                _blockCancelToken = new CancellationTokenSource();
                var prevHeader = await _blockRepository.GetNewestBlockHeader();
                if (prevHeader == null)
                {
                    if (!_buildGenesis)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        continue;
                    }
                    prevHeader = new BlockHeader() { BlockId = HeadKey, Height = 0 };
                    _logger.LogInformation($"Building genesis block");
                }
                var block = await AssembleBlock(prevHeader.BlockId, prevHeader.Height + 1, _blockCancelToken.Token);
                if (block != null)
                {
                    var recvResult = await _blockReciever.RecieveTail(block);
                    if (recvResult == PeerDataResult.Relay)
                        _peerNetwork.BroadcastTail(block);
                }
            }
        }

        private void PendingTransactionList_Changed(object sender, EventArgs e)
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

        private async Task<Block> AssembleBlock(byte[] prevBlock, uint height, CancellationToken cancellationToken)
        {
            var targetTxns = _pendingTransactionList.Get;
            targetTxns.Add(_blockbaseBuilder.Build(_builderKeys, targetTxns));
            var hashDict = HashTransactions(targetTxns, cancellationToken);
            var merkleRoot = await _merkleTreeBuilder.BuildTree(hashDict.Keys);

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
                    Timestamp = DateTime.UtcNow.Ticks,
                    Status = BlockStatus.Unconfirmed,
                    Version = _networkParameters.HeaderVersion,
                    Height = height,
                    PreviousBlock = prevBlock,
                    Difficulty = _networkParameters.Difficulty
                }
            };

            _logger.LogDebug($"Notarizing block {height}");
            await _blockNotary.ConfirmBlock(result, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug($"Cancelled building block {height}");
                return null;
            }

            _logger.LogDebug($"Built block {height} - {BitConverter.ToString(result.Header.BlockId)}");

            return result;
        }
                
        
                
        private IDictionary<byte[], TransactionEnvelope> HashTransactions(ICollection<TransactionEnvelope> transactions, CancellationToken cancellationToken)
        {
            var result = new ConcurrentDictionary<byte[], TransactionEnvelope>();
                        
            Parallel.ForEach(transactions, txn =>
            {
                var key = _transactionKeyResolver.ResolveKey(txn);
                result[key] = txn;
            });

            return result;
        }

    }
}
