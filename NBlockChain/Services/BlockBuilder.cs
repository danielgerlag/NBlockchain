using NBlockChain.Interfaces;
using NBlockChain.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NBlockChain.Services
{
    public class BlockBuilder<T> : IBlockBuilder<T>
        where T : AbstractTransaction
    {

        private readonly INetworkParameters _networkParameters;
        private readonly IHasher _hasher;
        private readonly IMerkleTreeBuilder _merkleTreeBuilder;
        private readonly ICollection<ITransactionValidator<T>> _validators;
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(true);

        private readonly Queue<T> _transactionQueue;                

        public BlockBuilder(IHasher hasher, IMerkleTreeBuilder merkleTreeBuilder, IEnumerable<ITransactionValidator<T>> validators, INetworkParameters networkParameters)
        {
            _networkParameters = networkParameters;
            _hasher = hasher;
            _validators = validators.ToArray();
            _merkleTreeBuilder = merkleTreeBuilder;
            _transactionQueue = new Queue<T>();         
        }

        public async Task<int> QueueTransaction(T transaction)
        {
            var result = 0;

            foreach (var validator in _validators)
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

        public async Task<Block<T>> BuildBlock(DateTime endTime, byte[] prevBlock)
        {            
            var targetTxns = SelectTransactions(endTime);
            var hashDict = HashTransactions(targetTxns);
            var merkleRoot = await _merkleTreeBuilder.BuildTree(hashDict.Keys);
                        
            var result = new Block<T>()
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


        private ICollection<T> SelectTransactions(DateTime endTime)
        {
            var targetTransactions = new List<T>();
            _resetEvent.WaitOne();
            try
            {
                while (_transactionQueue.Count() > 0)
                {
                    var peek = _transactionQueue.Peek();
                    if (peek.Timestamp > endTime.Ticks)
                        break;

                    targetTransactions.Add(_transactionQueue.Dequeue());
                }
            }
            finally
            {
                _resetEvent.Set();
            }

            return targetTransactions;
        }

        private IDictionary<byte[], T> HashTransactions(ICollection<T> transactions)
        {
            var result = new ConcurrentDictionary<byte[], T>();

            Parallel.ForEach(transactions, txn =>
            {
                var txnHeader = BitConverter.GetBytes(txn.Timestamp)
                    .Concat(BitConverter.GetBytes(txn.Version));

                var hash = _hasher.ComputeHash(txnHeader.Concat(txn.GetRawData()).ToArray());
                result[hash] = txn;
            });

            return result;            
        }

    }
}
