using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NBlockChain.Interfaces;
using NBlockChain.Models;

namespace NBlockChain.Services.PeerDiscovery
{
    public class StaticPeerDiscovery : IPeerDiscoveryService
    {
        private readonly string _peerStr;
        private readonly Guid _key;

        public StaticPeerDiscovery(string connectionString, Guid key)
        {
            _peerStr = connectionString;
            _key = key;
        }

        public Task<ICollection<PeerNode>> DiscoverPeers()
        {
            ICollection<PeerNode> result = new HashSet<PeerNode>();
            if (_peerStr != string.Empty)
                result.Add(new PeerNode() { ConnectionString = _peerStr });
            return Task.FromResult(result);
        }

        public async Task SharePeers(ICollection<PeerNode> peers)
        {
            await Task.Yield();
        }
    }
}
