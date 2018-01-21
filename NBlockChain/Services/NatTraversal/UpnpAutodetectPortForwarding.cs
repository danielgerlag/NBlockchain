using System;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Logging;
using NBlockchain.Interfaces;

namespace NBlockchain.Services.NatTraversal
{
    public class UpnpAutodetectPortForwarding : INatTraversal
    {
        private readonly string _description;
        private readonly IProvideUpnpDevice _upnpDeviceProvider;
        private readonly ILogger _logger;

        public UpnpAutodetectPortForwarding(string description, ILoggerFactory loggerFactory, IProvideUpnpDevice upnpDeviceProvider)
        {
            _description = description;
            _upnpDeviceProvider = upnpDeviceProvider;
            _logger = loggerFactory.CreateLogger<UpnpAutodetectPortForwarding>();
        }
        public string ConfigureNatTraversal(IPAddress ownAddress, int internalPort)
        {
            var ip = _upnpDeviceProvider.GetExternalIp();
            var allMappings = _upnpDeviceProvider.GetAllMappings();

            var existingMapping = allMappings.SingleOrDefault(m => m.PrivatePort == internalPort && m.Description == _description);
            if (existingMapping?.PrivateIP?.Equals(ownAddress) ?? false)
            {
                return $"{existingMapping.PublicIP}:{existingMapping.PublicPort}";
            }
            if (!existingMapping?.PrivateIP?.Equals(ownAddress) ?? false)
            {
                _logger.LogError($"The port {internalPort} is in use by another IP: {existingMapping.PrivateIP}");
                throw new Exception($"The port {internalPort} is in use by another IP: {existingMapping.PrivateIP}");
            }
            for (var nextPort = 49151; nextPort < 65535; nextPort++)
            {
                if (allMappings.Any(m => m.PublicPort == nextPort && !m.PrivateIP.Equals(ownAddress))) continue;
                _upnpDeviceProvider.CreateMapping(internalPort, nextPort, _description);
                return $"{ip}:{nextPort}";
            }
            _logger.LogError("No available ports found.");
            throw new Exception("No available ports found.");
        }
    }
}