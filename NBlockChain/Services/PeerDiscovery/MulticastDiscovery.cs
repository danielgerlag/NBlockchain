using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NBlockChain.Interfaces;
using NBlockChain.Models;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using Microsoft.Extensions.Logging;
using Polly;

namespace NBlockChain.Services.PeerDiscovery
{
    public class MulticastDiscovery : IPeerDiscoveryService
    {
        private readonly string _serviceId;
        private readonly string _multicastAddress;
        private readonly int _port;
        private readonly ILogger _logger;
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(5);

        private Task _advertiseTask;
        private CancellationTokenSource _advertiseCts;

        public MulticastDiscovery(string serviceId, string multicastAddress, int port, ILoggerFactory loggerFactory)
        {
            _serviceId = serviceId;
            _multicastAddress = multicastAddress;
            _port = port;
            _logger = loggerFactory.CreateLogger<MulticastDiscovery>();
        }

        public async Task AdvertiseGlobal(string connectionString)
        {            
        }

        public async Task AdvertiseLocal(string connectionString)
        {
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
                    _logger.LogDebug($"Advertising {connectionString}");
                    Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    IPAddress ip = IPAddress.Parse(_multicastAddress);
                    s.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(ip));
                    s.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 2);
                    IPEndPoint ipep = new IPEndPoint(ip, _port);
                    s.Connect(ipep);

                    while (!_advertiseCts.IsCancellationRequested)
                    {
                        var dataStr = _serviceId + connectionString;
                        s.Send(Encoding.ASCII.GetBytes(dataStr));
                        await Task.Delay(_interval);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
            });
        }

        public async Task<ICollection<KnownPeer>> DiscoverPeers()
        {
            _logger.LogDebug("Discovering peers");
            var result = new HashSet<KnownPeer>();

            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint ipep = new IPEndPoint(IPAddress.Any, _port);

            Policy.Handle<SocketException>()
                .WaitAndRetry((new[] { _interval, _interval }))
                .Execute(() => s.Bind(ipep));

            _logger.LogDebug($"Bound port {_port}");

            IPAddress ip = IPAddress.Parse(_multicastAddress);
            s.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(ip, IPAddress.Any));
            s.ReceiveTimeout = _interval.Milliseconds + 1000;
            DateTime pollUntil = DateTime.Now.Add(_interval);

            while (pollUntil > DateTime.Now)
            {
                byte[] b = new byte[1024];
                try
                {
                    int size = s.Receive(b);
                    string message = Encoding.ASCII.GetString(b, 0, size);
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
                    _logger.LogDebug(ex.Message);
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
            }
            s.Close();

            return result;
        }

        public async Task SharePeers(ICollection<KnownPeer> peers)
        {
        }
    }
}
