using NBlockchain.Models;
using NBlockchain.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ScratchPad
{
    public class TestTransactionValidator : TransactionValidator<TestTransaction>
    {
        protected override int Validate(TransactionEnvelope envelope, TestTransaction transaction, ICollection<TransactionEnvelope> siblings)
        {
            if (transaction.Message.Length > 1)
                return 0;

            return 1;
        }
    }
}
