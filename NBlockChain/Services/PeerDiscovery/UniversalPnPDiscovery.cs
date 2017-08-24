using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NBlockchain.Interfaces;
using NBlockchain.Models;

namespace NBlockchain.Services.PeerDiscovery
{
    public class UniversalPnPDiscovery : IPeerDiscoveryService
    {
        public Task AdvertiseGlobal(string connectionString)
        {
            throw new NotImplementedException();
        }

        public Task AdvertiseLocal(string connectionString)
        {
            throw new NotImplementedException();
        }

        public Task<ICollection<KnownPeer>> DiscoverPeers()
        {
            throw new NotImplementedException();
        }

        public Task SharePeers(ICollection<KnownPeer> peers)
        {
            throw new NotImplementedException();
        }
    }
}
