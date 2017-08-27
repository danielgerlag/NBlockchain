using DigitalCurrency.Repositories;
using DigitalCurrency.Transactions;
using NBlockchain.Models;
using NBlockchain.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCurrency.Rules
{
    public class BalanceRule : TransactionRule<TransferTransaction>
    {
        private readonly ITransactionRepository _txnRepo;

        public BalanceRule(ITransactionRepository txnRepo)
        {
            _txnRepo = txnRepo;
        }

        protected override int Validate(TransactionEnvelope envelope, TransferTransaction transaction, ICollection<TransactionEnvelope> siblings)
        {
            if (transaction.Amount < 0)
                return 1;

            var balance = _txnRepo.GetAccountBalance(envelope.Originator);
            if (transaction.Amount > balance)
                return 2;

            return 0;
        }
    }    
}
