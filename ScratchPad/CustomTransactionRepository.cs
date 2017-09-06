using LiteDB;
using Microsoft.Extensions.Logging;
using NBlockchain.Interfaces;
using NBlockchain.Models;
using NBlockchain.Services.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ScratchPad
{
    public class CustomTransactionRepository : TransactionRepository, ICustomTransactionRepository
    {
        public CustomTransactionRepository(ILoggerFactory loggerFactory, IDataConnection dataConnection)
            :base(loggerFactory, dataConnection)
        {
        }
                
        public decimal GetAccountBalance(string account)
        {
            var totalOut = Transactions
                .Find(Query.EQ("Entity.Originator", account))
                .Select(x => x.Entity.Transaction)
                .OfType<Transaction>()
                .Sum(x => x.Amount);
                        

            var totalIn = Transactions
                .Find(Query.EQ("Entity.Transaction.Destination", account))
                .Select(x => x.Entity.Transaction)
                .OfType<TestTransaction>()
                .Where(x => x.Destination == account)
                .Sum(x => x.Amount);

            return (totalIn - totalOut);
        }
    }
}
