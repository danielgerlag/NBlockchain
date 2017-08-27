using DigitalCurrency.Transactions;
using NBlockchain.Models;
using NBlockchain.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCurrency.Rules
{
    public class CoinbaseRule : TransactionRule<CoinbaseTransaction>
    {
        protected override int Validate(TransactionEnvelope envelope, CoinbaseTransaction transaction, ICollection<TransactionEnvelope> siblings)
        {
            if (transaction.Amount != -50)
                return 1;

            return 0;
        }
    }
}
