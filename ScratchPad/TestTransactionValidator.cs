using NBlockChain.Models;
using NBlockChain.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ScratchPad
{
    public class TestTransactionValidator : TransactionValidator<TestTransaction>
    {
        protected override async Task<int> Validate(TransactionEnvelope envelope, TestTransaction transaction)
        {
            if (transaction.Message.Length > 1)
                return await Task.FromResult(0);

            return 1;
        }
    }
}
