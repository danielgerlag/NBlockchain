using NBlockchain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace NBlockchain.Tests.Scenarios.Common
{
    [TransactionType("txn-v1")]
    public class TestTransaction : BlockTransaction
    {
        public string Data { get; set; }
    }
}
