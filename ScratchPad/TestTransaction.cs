using NBlockchain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ScratchPad
{
    [TransactionType("test-v1")]
    public class TestTransaction
    {
        public string Message { get; set; }
    }
}
