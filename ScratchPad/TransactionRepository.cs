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
            //new JsonCommand<decimal>("");
            //Database.RunCommand()
            //Builders<PersistedBlock>.Filter.

            var result = Blocks.Aggregate()

                //.Unwind<TransactionEnvelope>(new StringFieldDefinition<PersistedBlock>("Transactions"))
                //.Match(x => x.Originator == account)
                //.First();
                
                //.Match(new BsonDocument { { "Originator", account } })
                .Group(new BsonDocument { { "_id", BsonNull.Value }, { "sum", new BsonDocument("$sum", "$Transactions.Transaction.Amount") } })
                
                .ToList();


            var totalOut = Blocks.AsQueryable()
                .SelectMany(x => x.Transactions)
                .Where(x => x.Originator == account)
                .Select(x => x.Transaction)                
                .OfType<object>()
                .OfType<CoinbaseTransaction>()
                .Sum(x => x.Amount);

            var totalIn = 0;
                //Blocks.AsQueryable()
                //.SelectMany(x => x.Transactions)
                //.Where(x => x.TransactionType == "txn-v1")
                //.Select(x => x.Transaction)                
                //.Sum(x => (x as TestTransaction).Amount);

            return (totalIn - totalOut);
        }

    }
}
