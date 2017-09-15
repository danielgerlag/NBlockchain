using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DigitalCurrency.Transactions;
using NBlockchain.Interfaces;
using NBlockchain.Models;

namespace DigitalCurrency.Rules
{
    class CoinbaseBlockRule : IBlockRule
    {
        public bool TailRule => false;

        public Task<bool> Validate(Block block)
        {
            var coinbaseCount = block.Transactions.Count(x => x.Instructions.OfType<CoinbaseInstruction>().Any());
            return Task.FromResult(coinbaseCount == 1);
        }
    }
}
