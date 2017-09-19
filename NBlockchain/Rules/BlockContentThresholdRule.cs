using NBlockchain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NBlockchain.Models;

namespace NBlockchain.Rules
{
    public class BlockContentThresholdRule : IBlockRule
    {
        private readonly IUnconfirmedTransactionPool _unconfirmedTransactions;
        private readonly decimal _threshold;

        public bool TailRule => true;

        public BlockContentThresholdRule(IUnconfirmedTransactionPool unconfirmedTransactions, decimal threshold)
        {
            _unconfirmedTransactions = unconfirmedTransactions;
            _threshold = threshold;
        }
        
        public Task<bool> Validate(Block block)
        {
            var expected = _unconfirmedTransactions.Get;
            if (expected.Count == 0)
                return Task.FromResult(true);
            
            var count = expected.Count(txn => block.Transactions.Any(actual => actual.TransactionId.SequenceEqual(txn.TransactionId)));
            var ratio = (decimal)count / (decimal)expected.Count;
            return Task.FromResult(ratio >= _threshold);
        }
        
    }
}
