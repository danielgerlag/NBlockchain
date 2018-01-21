using System;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Logging;
using NBlockchain.Interfaces;

namespace NBlockchain.Services.NatTraversal
{
    public class UpnpStaticPortForwarding : INatTraversal
    {
        private readonly string _description;
        private readonly int _externalPort;
        private readonly IProvideUpnpDevice _upnpDeviceProvider;
        private readonly ILogger _logger;

        public UpnpStaticPortForwarding(string description, int externalPort, ILoggerFactory loggerFactory, IProvideUpnpDevice upnpDeviceProvider)
        {
            _description = description;
            _externalPort = externalPort;
            _upnpDeviceProvider = upnpDeviceProvider;
            _logger = loggerFactory.CreateLogger<UpnpStaticPortForwarding>();
        }
        public string ConfigureNatTraversal(IPAddress ownAddress, int internalPort)
        {
            var ip = _upnpDeviceProvider.GetExternalIp();
            var allMappings = _upnpDeviceProvider.GetAllMappings();

            var existingMapping = allMappings.SingleOrDefault(m =>m.PrivatePort == internalPort && m.Description == _description);
            if (existingMapping?.PublicIP?.Equals(ownAddress) ?? false)
            {
                return $"{existingMapping.PublicIP}:{existingMapping.PublicPort}";
            }
            if (!existingMapping?.PrivateIP?.Equals(ownAddress) ?? false)
            {
                _logger.LogError($"The port {internalPort} is in use by another IP: {existingMapping.PrivateIP}");
                throw new Exception($"The port {internalPort} is in use by another IP: {existingMapping.PrivateIP}");
            }
            _upnpDeviceProvider.CreateMapping(internalPort, _externalPort, _description);
            return $"{ip}:{_externalPort}";
        }
    }
}