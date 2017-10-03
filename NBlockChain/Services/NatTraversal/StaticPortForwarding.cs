using System.Net;
using NBlockchain.Interfaces;

namespace NBlockchain.Services.NatTraversal
{
    public class StaticPortForwarding : INatTraversal
    {
        private readonly int _staticExternalPort;
        private readonly IProvideUpnpDevice _upnpDeviceProvider;

        public StaticPortForwarding(int staticExternalPort, IProvideUpnpDevice upnpDeviceProvider)
        {
            _staticExternalPort = staticExternalPort;
            _upnpDeviceProvider = upnpDeviceProvider;
        }
        public string ConfigureNatTraversal(IPAddress ownAddress, int internalPort)
        {
            var ip = _upnpDeviceProvider.GetExternalIp();
            return $"{ip}:{_staticExternalPort}";
        }
    }
}