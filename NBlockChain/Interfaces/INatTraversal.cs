using System.Net;
using System.Threading.Tasks;
using NBlockchain.Services.Net;
using Open.Nat;

namespace NBlockchain.Interfaces
{
    public interface INatTraversal
    {
        string ConfigureNatTraversal(IPAddress ownAddress, int internalPort);
    }
}