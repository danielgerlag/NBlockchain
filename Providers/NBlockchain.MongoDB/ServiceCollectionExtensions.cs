using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Driver;
using NBlockchain.Interfaces;
using NBlockchain.MongoDB;
using NBlockchain.MongoDB.Services;
using NBlockchain.Models;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static BlockchainMongoOptions UseMongoDB(this BlockchainOptions options, string mongoUrl, string databaseName)
        {
            options.Services.AddTransient<IMongoDatabase>(sp =>
            {
                var client = new MongoClient(mongoUrl);
                return client.GetDatabase(databaseName);
            });

            options.UseBlockRepository<MongoBlockRepository>();
            options.Services.AddTransient<IInstructionRepository, MongoInstructionRepository>();
            options.AddPeerDiscovery<MongoPeerDirectory>();

            return new BlockchainMongoOptions(options, mongoUrl, databaseName);
        }
    }

    public class BlockchainMongoOptions
    {
        private readonly BlockchainOptions _options;
        private readonly string _mongoUrl;
        private readonly string _databaseName;

        public BlockchainMongoOptions(BlockchainOptions options, string mongoUrl, string databaseName)
        {
            _options = options;
            _mongoUrl = mongoUrl;
            _databaseName = databaseName;
        }

        public BlockchainMongoOptions UseInstructionRepository<TService, TImplementation>()
            where TImplementation : MongoInstructionRepository, TService
            where TService : class
        {
            _options.Services.AddTransient<TService, TImplementation>();
            return this;
        }
    }
}
