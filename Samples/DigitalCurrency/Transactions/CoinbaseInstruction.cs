using NBlockchain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCurrency.Transactions
{
    [InstructionType("coinbase-v1")]
    public class CoinbaseInstruction : ValueInstruction
    {
    }
}
