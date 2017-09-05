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
            var totalOut = Blocks
                .Find(Query.EQ("Entity.Transactions.Originator", account))
                .SelectMany(x => x.Entity.Transactions)
                .Where(x => x.Originator == account)
                .Select(x => x.Transaction)
                .OfType<Transaction>()
                .Sum(x => x.Amount);
                        

            var totalIn = Blocks
                //.Find(x => x.Entity.Transactions.Select(y => y.Transaction).OfType<TestTransaction>().Count(y => y.Destination == account) > 0)
                .Find(Query.EQ("Entity.Transactions.Transaction.Destination", account))
                .SelectMany(x => x.Entity.Transactions)                
                .Select(x => x.Transaction)
                .OfType<TestTransaction>()
                .Where(x => x.Destination == account)
                .Sum(x => x.Amount);

            return (totalIn - totalOut);
        }
    }
}
