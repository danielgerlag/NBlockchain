using System.Net;

namespace NBlockchain.Interfaces
{
    public interface IOwnAddressResolver
    {
        IPAddress ResolvePreferredLocalAddress();
    }
}