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

        private readonly IServiceProvider _serviceProvider;
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(true);

        private readonly Queue<TransactionEnvelope> _transactionQueue;                

        public BlockBuilder(ITransactionKeyResolver transactionKeyResolver, IMerkleTreeBuilder merkleTreeBuilder, INetworkParameters networkParameters, IServiceProvider serviceProvider, IEnumerable<ITransactionValidator> validators)
        {
            _networkParameters = networkParameters;
            _serviceProvider = serviceProvider;
            _validators = validators.ToList();
            _transactionKeyResolver = transactionKeyResolver;
            _merkleTreeBuilder = merkleTreeBuilder;
            _transactionQueue = new Queue<TransactionEnvelope>();         
        }

        public async Task<int> QueueTransaction(TransactionEnvelope transaction)
        {
            var result = 0;

            //_serviceProvider.
            foreach (var validator in _validators.Where(v => v.TransactionType == transaction.TransactionType))
                result = result & await validator.Validate(transaction);

            if (result != 0)
                return result;

            _resetEvent.WaitOne();
            try
            {                
                _transactionQueue.Enqueue(transaction);

                return result;
            }
            finally
            {
                _resetEvent.Set();
            }

        }

        public async Task<Block> BuildBlock(byte[] prevBlock)
        {            
            var targetTxns = SelectTransactions();
            var hashDict = HashTransactions(targetTxns);
            var merkleRoot = await _merkleTreeBuilder.BuildTree(hashDict.Keys);
                        
            var result = new Block()
            {
                Transactions = targetTxns,                
                MerkleRootNode = merkleRoot,
                Header = new BlockHeader()
                {
                    MerkelRoot = merkleRoot.Value,
                    Timestamp = DateTime.UtcNow.Ticks,
                    Status = BlockStatus.Closed,
                    Version = _networkParameters.TransactionVersion,
                    PreviousBlock = prevBlock
                }
            };

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
                    //var peek = _transactionQueue.Peek();
                    //if (peek.Timestamp > endTime.Ticks)
                    //    break;

                    targetTransactions.Add(_transactionQueue.Dequeue());
                }
            }
            finally
            {
                _resetEvent.Set();
            }

            return targetTransactions;
        }

        private IDictionary<byte[], TransactionEnvelope> HashTransactions(ICollection<TransactionEnvelope> transactions)
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
