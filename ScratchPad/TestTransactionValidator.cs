using NBlockchain.Models;
using NBlockchain.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ScratchPad
{
    public class TestTransactionValidator : TransactionRule<TestTransaction>
    {
        private readonly ITransactionRepository _txnRepo;

        public TestTransactionValidator(ITransactionRepository txnRepo)
        {
            _txnRepo = txnRepo;
        }

        protected override int Validate(TransactionEnvelope envelope, TestTransaction transaction, ICollection<TransactionEnvelope> siblings)
        {
            if (transaction.Amount < 0)
                return 1;

            var balance = _txnRepo.GetAccountBalance(envelope.Originator);
            if (transaction.Amount > balance)
                return 2;

            return 0;
        }
    }

    public class CoinbaseTransactionValidator : TransactionRule<CoinbaseTransaction>
    {
        protected override int Validate(TransactionEnvelope envelope, CoinbaseTransaction transaction, ICollection<TransactionEnvelope> siblings)
        {
            if (transaction.Amount != -50)
                return 1;

            return 0;
        }
    }
}
