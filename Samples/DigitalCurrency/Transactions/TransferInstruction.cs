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

        public override ICollection<byte[]> ExtractSignableElements()
        {
            var result = base.ExtractSignableElements();
            result.Add(Destination);
            return result;
        }
    }
}
