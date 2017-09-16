using MongoDB.Bson;
using MongoDB.Driver;
using NBlockchain.MongoDB.Models;
using NBlockchain.MongoDB.Services;
using System;
using System.Collections.Generic;
using System.Text;
using NBlockchain.Interfaces;

namespace DigitalCurrency.Repositories.Mongo
{
    public class CustomMongoInstructionRepository : MongoInstructionRepository, ICustomInstructionRepository
    {

        private readonly IAddressEncoder _addressEncoder;

        public CustomMongoInstructionRepository(IMongoDatabase database, IAddressEncoder addressEncoder)
            : base(database)
        {
            _addressEncoder = addressEncoder;
        }

        protected override void CreateIndexes()
        {
        }

        public decimal GetAccountBalance(string address)
        {
            var publicKeyHash = _addressEncoder.ExtractPublicKeyHash(address);

            var totalOut = 0;
            var totalIn = 0;
            
            var outQry = MainChain.Aggregate()
                .Unwind(x => x.Transactions)
                .Unwind<PersistedInstruction>(new StringFieldDefinition<BsonDocument>("Transactions.Instructions"))
                .Match(new BsonDocument("Transactions.Instructions.Statistics.PublicKeyHash", publicKeyHash))
                .Group(new BsonDocument { { "_id", BsonNull.Value }, { "sum", new BsonDocument("$sum", "$Transactions.Instructions.Entity.Amount") } })
                .SingleOrDefault();

            if (outQry != null)
            {
                if (outQry.TryGetValue("sum", out var bOut))
                    totalOut = bOut.AsInt32;
            }

            var inQry = MainChain.Aggregate()
                .Unwind(x => x.Transactions)
                .Unwind<PersistedInstruction>(new StringFieldDefinition<BsonDocument>("Transactions.Instructions"))
                .Match(new BsonDocument("Transactions.Instructions.Entity.Destination", publicKeyHash))
                .Group(new BsonDocument { { "_id", BsonNull.Value }, { "sum", new BsonDocument("$sum", "$Transactions.Instructions.Entity.Amount") } })
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
