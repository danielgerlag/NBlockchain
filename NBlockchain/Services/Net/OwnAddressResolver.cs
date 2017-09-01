using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using NBlockchain.Interfaces;

namespace NBlockchain.Services.Net
{
    public class OwnAddressResolver : IOwnAddressResolver
    {
        private readonly ILogger _logger;

        public OwnAddressResolver(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<OwnAddressResolver>();
        }

        public IPAddress ResolvePreferredLocalAddress()
        {
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    var endPoint = socket.LocalEndPoint as IPEndPoint;
                    return endPoint.Address;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null;
            }
        }
    }
}
