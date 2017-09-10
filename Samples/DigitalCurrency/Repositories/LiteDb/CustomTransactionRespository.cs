using DigitalCurrency.Transactions;
using LiteDB;
using Microsoft.Extensions.Logging;
using NBlockchain.Interfaces;
using NBlockchain.Services.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DigitalCurrency.Repositories.LiteDb
{
    public class CustomTransactionRepository : TransactionRepository, ICustomTransactionRepository
    {
        public CustomTransactionRepository(ILoggerFactory loggerFactory, IDataConnection dataConnection)
            : base(loggerFactory, dataConnection)
        {
        }

        public decimal GetAccountBalance(string account)
        {
            var totalOut = Transactions
                .Find(Query.EQ("Entity.Originator", account))
                .Select(x => x.Entity.Transaction)
                .OfType<ValueTransaction>()
                .Sum(x => x.Amount);


            var totalIn = Transactions
                .Find(Query.EQ("Entity.Transaction.Destination", account))
                .Select(x => x.Entity.Transaction)
                .OfType<TransferTransaction>()
                .Where(x => x.Destination == account)
                .Sum(x => x.Amount);

            return (totalIn - totalOut);
        }
    }
}
