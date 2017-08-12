using NBlockChain.Interfaces;
using NBlockChain.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace NBlockChain.Services
{
    public class BlockBuilder : IBlockBuilder
    {

        private readonly INetworkParameters _networkParameters;
        private readonly ITransactionKeyResolver _transactionKeyResolver;
        private readonly IMerkleTreeBuilder _merkleTreeBuilder;
        private readonly ICollection<ITransactionValidator> _validators;
        private readonly IEnumerable<ValidTransactionType> _validTxnTypes;
        private readonly IAddressEncoder _addressEncoder;
        private readonly ISignatureService _signatureService;
        private readonly IBlockbaseTransactionBuilder _blockbaseBuilder;
        private readonly IBlockNotarizer _blockNotarizer;
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(true);
        private readonly Queue<TransactionEnvelope> _transactionQueue;

        public BlockBuilder(ITransactionKeyResolver transactionKeyResolver, IMerkleTreeBuilder merkleTreeBuilder, INetworkParameters networkParameters, IEnumerable<ITransactionValidator> validators, IAddressEncoder addressEncoder, ISignatureService signatureService, IBlockbaseTransactionBuilder blockbaseBuilder, IEnumerable<ValidTransactionType> validTxnTypes, IBlockNotarizer blockNotarizer)
        {
            _networkParameters = networkParameters;
            _addressEncoder = addressEncoder;
            _signatureService = signatureService;
            _blockbaseBuilder = blockbaseBuilder;
            _validTxnTypes = validTxnTypes;
            _blockNotarizer = blockNotarizer;
            _validators = validators.ToList();
            _transactionKeyResolver = transactionKeyResolver;
            _merkleTreeBuilder = merkleTreeBuilder;
            _transactionQueue = new Queue<TransactionEnvelope>();         
        }

        public async Task QueueTransaction(TransactionEnvelope transaction)
        {
            _resetEvent.WaitOne();
            try
            {                
                _transactionQueue.Enqueue(transaction);
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public void FlushQueue()
        {
            _resetEvent.WaitOne();
            try
            {
                _transactionQueue.Clear();
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public async Task<Block> BuildBlock(byte[] prevBlock, uint height, KeyPair builderKeys, CancellationToken cancellationToken)
        {            
            var targetTxns = SelectTransactions();
            targetTxns.Add(_blockbaseBuilder.Build(builderKeys, targetTxns));
            var hashDict = HashTransactions(targetTxns, cancellationToken);
            var merkleRoot = await _merkleTreeBuilder.BuildTree(hashDict.Keys);

            if (cancellationToken.IsCancellationRequested)
                return null;
                        
            var result = new Block()
            {
                Transactions = targetTxns,                
                MerkleRootNode = merkleRoot,
                Header = new BlockHeader()
                {
                    MerkelRoot = merkleRoot.Value,
                    Timestamp = DateTime.UtcNow.Ticks,
                    Status = BlockStatus.Closed,
                    Version = _networkParameters.HeaderVersion,
                    Height = height,
                    PreviousBlock = prevBlock
                }
            };

            await _blockNotarizer.Notarize(result, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return null;

            return result;
        }


        private ICollection<TransactionEnvelope> SelectTransactions()
        {
            var targetTransactions = new List<TransactionEnvelope>();
            _resetEvent.WaitOne();
            try
            {
                while (_transactionQueue.Any())
                {
                    var txn = _transactionQueue.Dequeue();

                    if (targetTransactions.All(x => x.OriginKey != txn.OriginKey && x.Originator != txn.Originator))
                        targetTransactions.Add(txn);
                }
            }
            finally
            {
                _resetEvent.Set();
            }

            return targetTransactions;
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
