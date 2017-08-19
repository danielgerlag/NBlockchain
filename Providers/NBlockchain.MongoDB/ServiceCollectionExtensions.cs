using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Driver;
using NBlockchain.MongoDB;
using NBlockChain.Models;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static BlockchainOptions UseMongoDB(this BlockchainOptions options, string mongoUrl, string databaseName)
        {
            options.UseBlockRepository(sp =>
            {
                var client = new MongoClient(mongoUrl);
                var db = client.GetDatabase(databaseName);
                return new MongoBlockRepository(db);
            });

            options.AddPeerDiscovery(sp =>
            {
                var client = new MongoClient(mongoUrl);
                var db = client.GetDatabase(databaseName);
                return new MongoPeerDirectory(db);
            });

            return options;
        }
    }
}
