using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCurrency.Repositories
{
    public interface ICustomTransactionRepository
    {
        decimal GetAccountBalance(string account);
    }
}
