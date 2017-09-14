﻿using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NBlockchain.MongoDB.Models;
using NBlockchain.Interfaces;
using NBlockchain.Models;

namespace NBlockchain.MongoDB.Services
{
    public class MongoInstructionRepository : IInstructionRepository
    {
        protected readonly IMongoDatabase Database;
        protected IMongoCollection<PersistedBlock> Blocks => Database.GetCollection<PersistedBlock>("nbc.blocks");

        public MongoInstructionRepository(IMongoDatabase database)
        {
            Database = database;
            EnsureIndexes();
        }

        protected virtual void CreateIndexes()
        {
        }
        

        static bool _indexesCreated = false;
        private void EnsureIndexes()
        {
            if (!_indexesCreated)
            {
                CreateIndexes();
                _indexesCreated = true;
            }
        }

        public Task<bool> HaveInstruction(byte[] instructionId)
        {
            var result = Blocks.AsQueryable().Any(x => x.Transactions.SelectMany(y => y.Instructions).Any(y => y.Id == instructionId));
            return Task.FromResult(result);
        }
    }
    
}
