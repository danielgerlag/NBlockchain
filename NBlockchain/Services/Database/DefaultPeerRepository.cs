using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using Microsoft.Extensions.Logging;
using NBlockchain.Interfaces;
using NBlockchain.Models;

namespace NBlockchain.Services.Database
{
    public class DefaultPeerRepository : IPeerDiscoveryService
    {
        private readonly ILogger _logger;
        private readonly IDataConnection _connection;

        public DefaultPeerRepository(IDataConnection connection, ILoggerFactory loggerFactory)
        {
            _connection = connection;
            _logger = loggerFactory.CreateLogger<DefaultPeerRepository>();
        }

        protected LiteCollection<PersistedEntity<KnownPeer, ObjectId>> Peers => _connection.Database.GetCollection<PersistedEntity<KnownPeer, ObjectId>>("Peers");

        public async Task<ICollection<KnownPeer>> DiscoverPeers()
        {
            var result = Peers.FindAll().Select(x => x.Entity);
            return result.ToList();
        }

        public Task SharePeers(ICollection<KnownPeer> peers)
        {
            foreach (var peer in peers)
            {
                var query = Peers.Find(x => x.Entity.ConnectionString == peer.ConnectionString);
                if (query.Any())
                {
                    var existing = query.First();
                    existing.Entity = peer;
                    Peers.Update(existing);
                }
                else
                {
                    Peers.Insert(new PersistedEntity<KnownPeer, ObjectId>(peer));
                }
            }

            return Task.CompletedTask;
        }

        public Task AdvertiseGlobal(string connectionString)
        {
            return Task.CompletedTask;
        }

        public Task AdvertiseLocal(string connectionString)
        {
            return Task.CompletedTask;
        }
    }
}
