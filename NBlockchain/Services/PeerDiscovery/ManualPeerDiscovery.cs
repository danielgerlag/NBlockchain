using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NBlockchain.Interfaces;
using NBlockchain.Models;

namespace NBlockchain.Services.PeerDiscovery
{
    public class ManualPeerDiscovery : IPeerDiscoveryService
    {
        private static ICollection<string> _internalList = new HashSet<string>();
        
        public async Task AdvertiseGlobal(string connectionString)
        {            
        }

        public async Task AdvertiseLocal(string connectionString)
        {
            _internalList.Add(connectionString);
        }

        public async Task<ICollection<KnownPeer>> DiscoverPeers()
        {
            var result = _internalList.Select(x => new KnownPeer() {ConnectionString = x}).ToList();
            return result;
        }

        public async Task SharePeers(ICollection<KnownPeer> peers)
        {
            await Task.Yield();
        }
    }
}
