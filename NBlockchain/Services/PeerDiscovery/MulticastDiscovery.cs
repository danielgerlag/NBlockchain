using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using Microsoft.Extensions.Logging;
using NBlockchain.Interfaces;
using NBlockchain.Models;
using Polly;

namespace NBlockchain.Services.PeerDiscovery
{
    public class MulticastDiscovery : IPeerDiscoveryService
    {
        private readonly string _serviceId;
        private readonly string _multicastAddress;
        private readonly int _port;
        private readonly ILogger _logger;
        private readonly IOwnAddressResolver _ownAddressResolver;
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(5);

        private Task _advertiseTask;
        private CancellationTokenSource _advertiseCts;

        public MulticastDiscovery(string serviceId, string multicastAddress, int port, ILoggerFactory loggerFactory, IOwnAddressResolver ownAddressResolver)
        {
            _serviceId = serviceId;
            _multicastAddress = multicastAddress;
            _port = port;
            _ownAddressResolver = ownAddressResolver;
            _logger = loggerFactory.CreateLogger<MulticastDiscovery>();
        }

        public async Task AdvertiseGlobal(string connectionString)
        {            
        }

        public async Task AdvertiseLocal(string connectionString)
        {
            _logger.LogInformation($"Advertising to peers {connectionString}");
            if (_advertiseTask != null)
            {
                _advertiseCts.Cancel();
                await _advertiseTask;
            }

            _advertiseCts = new CancellationTokenSource();
            _advertiseTask = Task.Factory.StartNew(async () =>
            {
                try
                {
                    var dataStr = _serviceId + connectionString;
                    var data = Encoding.ASCII.GetBytes(dataStr);
                    using (var udpClient = new UdpClient(AddressFamily.InterNetwork))
                    {
                        var address = IPAddress.Parse(_multicastAddress);
                        var ipEndPoint = new IPEndPoint(address, _port);
                        udpClient.JoinMulticastGroup(address);

                        while (!_advertiseCts.IsCancellationRequested)
                        {
                            _logger.LogDebug($"Advertising {connectionString}");
                            await udpClient.SendAsync(data, data.Length, ipEndPoint);
                            await Task.Delay(_interval);
                        }
                        udpClient.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error advertising - {ex.Message}");
                }
            });
        }

        public async Task<ICollection<KnownPeer>> DiscoverPeers()
        {
            _logger.LogInformation("Discovering peers");

            var result = new HashSet<KnownPeer>();

            try
            {
                var udpClient = new UdpClient(_port);

                var ownAddress = _ownAddressResolver.ResolvePreferredLocalAddress();
                udpClient.JoinMulticastGroup(IPAddress.Parse(_multicastAddress), ownAddress);

                udpClient.Client.ReceiveTimeout = Convert.ToInt32(_interval.TotalMilliseconds + 1000);

                DateTime pollUntil = DateTime.Now.Add(_interval);

                while (pollUntil > DateTime.Now)
                {
                    byte[] b = new byte[1024];
                    try
                    {
                        //var ipEndPoint = new IPEndPoint(IPAddress.Any, 0);
                        var data = await udpClient.ReceiveAsync(); //(ref ipEndPoint);                    
                        string message = Encoding.ASCII.GetString(data.Buffer);
                        _logger.LogDebug($"rx message {message}");
                        if (message.StartsWith(_serviceId))
                        {
                            var connStr = message.Remove(0, _serviceId.Length);
                            result.Add(new KnownPeer()
                            {
                                ConnectionString = connStr,
                                LastContact = DateTime.Now
                            });
                        }
                    }
                    catch (SocketException ex)
                    {
                        _logger.LogError($"SocketException polling for peers - {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Exception polling for peers - {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error joining multicast - {ex.Message}");
            }

            return result;
        }

        public async Task SharePeers(ICollection<KnownPeer> peers)
        {
        }
    }
}
