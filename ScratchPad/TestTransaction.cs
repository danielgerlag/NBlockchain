using NBlockchain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ScratchPad
{
    
    public class Transaction
    {
        public decimal Amount { get; set; }
    }

    [TransactionType("txn-v1")]
    public class TestTransaction : Transaction
    {
        public string Message { get; set; }

        public string Destination { get; set; }
    }

    [TransactionType("coinbase-v1")]
    public class CoinbaseTransaction : Transaction
    {        
    }
}
