using NBlockchain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCurrency.Transactions
{
    [TransactionType("coinbase-v1")]
    public class CoinbaseTransaction : ValueTransaction
    {
    }
}
