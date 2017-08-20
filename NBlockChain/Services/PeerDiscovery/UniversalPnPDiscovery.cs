using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NBlockChain.Interfaces;
using NBlockChain.Models;

namespace NBlockChain.Services.PeerDiscovery
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
