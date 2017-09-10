using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NBlockchain.Interfaces;
using NBlockchain.Models;

namespace NBlockchain.MongoDB.Services
{
    public class MongoPeerDirectory : IPeerDiscoveryService
    {
        private readonly IMongoDatabase _database;

        public MongoPeerDirectory(IMongoDatabase database)
        {
            _database = database;
            //CreateIndexes(this);
        }
        
        private IMongoCollection<MongoPeerNode> Peers => _database.GetCollection<MongoPeerNode>("nbc.peers");

        
        public async Task<ICollection<KnownPeer>> DiscoverPeers()
        {
            var query = Peers.Find(x => true);
            var raw = query.ToList();
            return raw.Cast<KnownPeer>().ToList();
        }

        public async Task SharePeers(ICollection<KnownPeer> peers)
        {
            foreach (var peer in peers)
            {
                var query = Peers.Find(x => x.ConnectionString == peer.ConnectionString);
                if (query.Any())
                {
                    var existing = query.First();
                    Peers.ReplaceOne(x => x.Id == existing.Id, new MongoPeerNode(existing.Id, peer));
                }
                else
                {
                    Peers.InsertOne(new MongoPeerNode(peer));
                }
            }
        }

        static bool indexesCreated = false;
        static void CreateIndexes(MongoPeerDirectory instance)
        {
            if (!indexesCreated)
            {
                //TODO
                indexesCreated = true;
            }
        }

        public async Task AdvertiseGlobal(string connectionString)
        {         
        }

        public async Task AdvertiseLocal(string connectionString)
        {
        }
    }

    public class MongoPeerNode : KnownPeer
    {
        public ObjectId Id { get; set; }

        public MongoPeerNode()
        {
        }

        public MongoPeerNode(KnownPeer node)
        {
            this.ConnectionString = node.ConnectionString;
            this.LastContact = node.LastContact;
            this.IsSelf = node.IsSelf;
            this.NodeId = node.NodeId;
        }

        public MongoPeerNode(ObjectId id, KnownPeer node)
        {
            this.Id = id;
            this.ConnectionString = node.ConnectionString;
            this.LastContact = node.LastContact;
            this.IsSelf = node.IsSelf;
            this.NodeId = node.NodeId;
        }
    }
}
