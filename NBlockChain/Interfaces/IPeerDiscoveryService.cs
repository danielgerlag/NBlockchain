using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NBlockChain.Models;

namespace NBlockChain.Interfaces
{
    public interface IPeerDiscoveryService
    {
        Task<ICollection<PeerNode>> DiscoverPeers();

        Task SharePeers(ICollection<PeerNode> peers);

    }
}
