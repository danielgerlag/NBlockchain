using NBlockchain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCurrency.Transactions
{
    public abstract class ValueTransaction : BlockTransaction
    {
        public int Amount { get; set; }
    }
}
