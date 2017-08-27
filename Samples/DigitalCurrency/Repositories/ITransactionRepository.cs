using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCurrency.Repositories
{
    public interface ITransactionRepository
    {
        decimal GetAccountBalance(string account);
    }
}
