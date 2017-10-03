using System.Net;
using NBlockchain.Interfaces;

namespace NBlockchain.Services.NatTraversal
{
    public class NoTraversal : INatTraversal
    {
        public string ConfigureNatTraversal(IPAddress ownAddress, int internalPort)
        {
            return $"{ownAddress}:{internalPort}";
        }
    }
}