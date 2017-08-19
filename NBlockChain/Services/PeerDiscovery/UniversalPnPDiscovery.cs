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
        public Task<ICollection<PeerNode>> DiscoverPeers()
        {
            throw new NotImplementedException();
        }

        public Task SharePeers(ICollection<PeerNode> peers)
        {
            throw new NotImplementedException();
        }
    }
}
