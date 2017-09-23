using DigitalCurrency.Transactions;
using NBlockchain.Models;
using NBlockchain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NBlockchain.Rules;
using NBlockchain.Interfaces;

namespace DigitalCurrency.Rules
{
    public class CoinbaseTransactionRule : ITransactionRule
    {
        public int Validate(Transaction transaction, ICollection<Transaction> siblings)
        {
            if (transaction.Instructions.OfType<CoinbaseInstruction>().Count() == 0)
                return 0;

            var coinbaseTotal = transaction.Instructions.OfType<CoinbaseInstruction>().Sum(x => x.Amount);

            if (coinbaseTotal != -50)
                return 1;

            return 0;
        }
    }
}
