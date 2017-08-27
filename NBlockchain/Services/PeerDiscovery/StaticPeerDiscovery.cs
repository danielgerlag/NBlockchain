using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NBlockchain.Interfaces;
using NBlockchain.Models;

namespace NBlockchain.Services.PeerDiscovery
{
    public class StaticPeerDiscovery : IPeerDiscoveryService
    {
        private readonly string _peerStr;
        
        public StaticPeerDiscovery(string connectionString)
        {
            _peerStr = connectionString;
        }

        public async Task AdvertiseGlobal(string connectionString)
        {            
        }

        public async Task AdvertiseLocal(string connectionString)
        {
        }

        public Task<ICollection<KnownPeer>> DiscoverPeers()
        {
            ICollection<KnownPeer> result = new HashSet<KnownPeer>();
            if (_peerStr != string.Empty)
                result.Add(new KnownPeer() { ConnectionString = _peerStr });
            return Task.FromResult(result);
        }

        public async Task SharePeers(ICollection<KnownPeer> peers)
        {
            await Task.Yield();
        }
    }
}
