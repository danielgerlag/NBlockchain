using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCurrency.Transactions
{
    public abstract class ValueTransaction
    {
        public int Amount { get; set; }
    }
}
