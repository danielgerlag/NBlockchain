using System;
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
    public abstract class MongoTransactionRepository
    {
        protected readonly IMongoDatabase Database;
        protected IMongoCollection<PersistedBlock> Blocks => Database.GetCollection<PersistedBlock>("nbc.blocks");

        protected MongoTransactionRepository(IMongoDatabase database)
        {
            Database = database;
            EnsureIndexes();
        }

        protected abstract void CreateIndexes();
        

        static bool _indexesCreated = false;
        private void EnsureIndexes()
        {
            if (!_indexesCreated)
            {
                
                _indexesCreated = true;
            }
        }
    }
    
}
