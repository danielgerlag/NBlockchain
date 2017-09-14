using NBlockchain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCurrency.Transactions
{    
    [InstructionType("txn-v1")]
    public class TransferInstruction : ValueInstruction
    {
        public string Message { get; set; }

        public byte[] Destination { get; set; }
    }
}
