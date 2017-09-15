using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCurrency.Repositories
{
    public interface ICustomInstructionRepository
    {
        decimal GetAccountBalance(string address);
    }
}
