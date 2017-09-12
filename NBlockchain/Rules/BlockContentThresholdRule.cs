using NBlockchain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using NBlockchain.Models;

namespace NBlockchain.Rules
{
    public class BlockContentThresholdRule : IBlockRule
    {
        private readonly IPendingTransactionList _pendingTransactions;
        private readonly decimal _threshold;

        public bool TailRule => true;

        public BlockContentThresholdRule(IPendingTransactionList pendingTransactions, decimal threshold)
        {
            _pendingTransactions = pendingTransactions;
            _threshold = threshold;
        }
        
        public bool Validate(Block block)
        {
            var expected = _pendingTransactions.Get;
            if (expected.Count == 0)
                return true;
            
            var count = expected.Count(txn => block.Transactions.Any(actual => actual.OriginKey == txn.OriginKey));
            var ratio = (decimal)count / (decimal)expected.Count;
            return (ratio >= _threshold);
        }
        
    }
}
