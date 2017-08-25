using MongoDB.Bson;
using MongoDB.Driver;
using NBlockchain.Models;
using NBlockchain.MongoDB.Models;
using NBlockchain.MongoDB.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ScratchPad
{
    public class TransactionRepository : MongoTransactionRepository, ITransactionRepository
    {
        public TransactionRepository(IMongoDatabase database)
            :base(database)
        {
        }

        protected override void CreateIndexes()
        {
            
        }

        public decimal GetAccountBalance(string account)
        {
            var totalOut = 0;
            var totalIn = 0;

            var outQry = Blocks.Aggregate()
                .Unwind(x => x.Transactions)
                .Match(new BsonDocument("Transactions.Originator", account))
                .Group(new BsonDocument {{"_id", BsonNull.Value}, {"sum", new BsonDocument("$sum", "$Transactions.Transaction.Amount")}})
                .SingleOrDefault();

            if (outQry != null)
            {
                if (outQry.TryGetValue("sum", out var bOut))
                    totalOut = bOut.AsInt32;
            }

            var inQry = Blocks.Aggregate()
                .Unwind(x => x.Transactions)
                .Match(new BsonDocument("Transactions.Transaction.Destination", account))
                .Group(new BsonDocument { { "_id", BsonNull.Value }, { "sum", new BsonDocument("$sum", "$Transactions.Transaction.Amount") } })
                .SingleOrDefault();

            if (inQry != null)
            {
                if (inQry.TryGetValue("sum", out var bIn))
                    totalIn = bIn.AsInt32;
            }

            return (totalIn - totalOut);
        }

    }
}
