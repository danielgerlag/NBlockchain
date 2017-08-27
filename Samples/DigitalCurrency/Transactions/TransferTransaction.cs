using NBlockchain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCurrency.Transactions
{    
    [TransactionType("txn-v1")]
    public class TransferTransaction : ValueTransaction
    {
        public string Message { get; set; }

        public string Destination { get; set; }
    }
}
