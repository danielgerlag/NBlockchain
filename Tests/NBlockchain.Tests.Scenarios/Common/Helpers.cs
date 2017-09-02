using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;

namespace NBlockchain.Tests.Scenarios.Common
{
    static class Helpers
    {
        public static uint GetFreePort()
        {
            const uint startRange = 1000;
            const uint endRange = 10000;
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpPorts = ipGlobalProperties.GetActiveTcpListeners();
            var udpPorts = ipGlobalProperties.GetActiveUdpListeners();

            var result = startRange;

            while (((tcpPorts.Any(x => x.Port == result)) || (udpPorts.Any(x => x.Port == result))) && result <= endRange)
                result++;

            if (result > endRange)
                throw new Exception("No ports found");

            return result;
        }
    }
}
