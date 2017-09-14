using DigitalCurrency.Repositories;
using DigitalCurrency.Transactions;
using NBlockchain.Models;
using NBlockchain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using NBlockchain.Rules;
using NBlockchain.Interfaces;

namespace DigitalCurrency.Rules
{
    public class BalanceRule : ITransactionRule
    {
        private readonly ICustomInstructionRepository _txnRepo;
        private readonly IAddressEncoder _addressEncoder;

        public BalanceRule(ICustomInstructionRepository txnRepo, IAddressEncoder addressEncoder)
        {
            _txnRepo = txnRepo;
            _addressEncoder = addressEncoder;
        }
        
        public int Validate(Transaction transaction, ICollection<Transaction> siblings)
        {
            if (transaction.Instructions.OfType<TransferInstruction>().Any(x => x.Amount < 0))
                return 1;
            
            foreach (var instruction in transaction.Instructions.OfType<TransferInstruction>())
            {
                var sourceAddr = _addressEncoder.EncodeAddress(instruction.PublicKey, 0);
                var balance = _txnRepo.GetAccountBalance(sourceAddr);
                if (instruction.Amount > balance)
                    return 2;
            }

            return 0;
        }
    }    
}
