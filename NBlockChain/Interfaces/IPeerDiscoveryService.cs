using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NBlockChain.Models;

namespace NBlockChain.Interfaces
{
    public interface IPeerDiscoveryService
    {
        Task<ICollection<KnownPeer>> DiscoverPeers();

        Task AdvertiseGlobal(string connectionString);

        Task AdvertiseLocal(string connectionString);

        Task SharePeers(ICollection<KnownPeer> peers);

    }
}
