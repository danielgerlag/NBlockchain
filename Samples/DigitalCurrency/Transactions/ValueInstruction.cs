using NBlockchain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCurrency.Transactions
{
    public abstract class ValueInstruction : Instruction
    {
        public int Amount { get; set; }

        public override ICollection<byte[]> ExtractSignableElements()
        {
            return new List<byte[]>() { BitConverter.GetBytes(Amount) };
        }
    }
}
