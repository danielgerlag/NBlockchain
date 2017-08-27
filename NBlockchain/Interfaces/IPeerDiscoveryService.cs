using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NBlockchain.Models;

namespace NBlockchain.Interfaces
{
    public interface IPeerDiscoveryService
    {
        Task<ICollection<KnownPeer>> DiscoverPeers();

        Task AdvertiseGlobal(string connectionString);

        Task AdvertiseLocal(string connectionString);

        Task SharePeers(ICollection<KnownPeer> peers);

    }
}
