using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace NBlockchain.Services.Net
{
    public class PeerConnection
    {
        private const int MaxMessageSize = 10240000;
        private const byte NetworkQualifier = 0;
        private const byte MessageQualifier = 1;

        private const byte IdentifyCommand = 0;
        private const byte PingCommand = 1;

        private readonly byte[] _serviceIdentifier;
        private readonly TcpClient _client;
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(true);
        private readonly Guid _localId;
        private readonly int _localVersion;
        private int _remoteVersion;


        private Guid _remoteId = Guid.NewGuid();
        private DateTime? _lastContact;
        private CancellationTokenSource _cancelToken = new CancellationTokenSource();
        private bool _pollExited = false;

        public event ReceiveMessage OnReceiveMessage;
        public event PeerEvent OnDisconnect;
        public event PeerEvent OnIdentify;
        public event PeerEvent OnUnresponsive;
        public event PeerException OnPeerException;

        public Guid RemoteId => _remoteId;

        public EndPoint RemoteEndPoint => _client?.Client?.RemoteEndPoint;
        public bool Outgoing { get; private set; }
        public string ConnectionString { get; private set; }
        public DateTime? LastContact => _lastContact;

        public long RequestCount { get; set; } = 0;
        public int DemeritPoints { get; set; } = 0;
        public TimeSpan QuietTimeout { get; set; } = TimeSpan.FromMinutes(10);
        
        public PeerConnection(string serviceIdentifier, int version, Guid nodeId)
        {
            _client = new TcpClient();
            _localId = nodeId;
            _localVersion = version;
            _serviceIdentifier = Encoding.UTF8.GetBytes(serviceIdentifier);
            Outgoing = true;
        }

        public PeerConnection(string serviceIdentifier, int version, Guid nodeId, TcpClient client)
        {
            _client = client;
            _localId = nodeId;
            _localVersion = version;
            _serviceIdentifier = Encoding.UTF8.GetBytes(serviceIdentifier);
            Outgoing = false;
        }

        public async Task Connect(string connectionString)
        {
            ConnectionString = connectionString;
            var uri = new Uri(connectionString);
            if (uri.Scheme != "tcp")
                throw new InvalidOperationException("Only tcp connections are possible");
                        
            await _client.ConnectAsync(uri.Host, uri.Port);
            SendIdentify();
        }

        public void Run()
        {
            Task.Factory.StartNew(Poll);
        }

        public void Send(byte command, byte[] data)
        {
            Send(MessageQualifier, command, data);
        }

        public void Send(byte qualifier, byte command, byte[] data)
        {
            _resetEvent.WaitOne();
            try
            {
                //_client.Client.SendTimeout = 1000;
                var headerLength = _serviceIdentifier.Length + 6;
                var lenBuffer = BitConverter.GetBytes(data.Length);
                var message = new byte[headerLength + data.Length];
                _serviceIdentifier.CopyTo(message, 0);
                lenBuffer.CopyTo(message, _serviceIdentifier.Length);
                message[_serviceIdentifier.Length + 4] = qualifier;
                message[_serviceIdentifier.Length + 5] = command;
                data.CopyTo(message, headerLength);
                _client.Client.Send(message);
            }
            catch (SocketException ex)
            {
                switch (ex.SocketErrorCode)
                {
                    case SocketError.ConnectionAborted:
                    case SocketError.ConnectionReset:
                    case SocketError.Disconnecting:
                    case SocketError.HostDown:
                    case SocketError.NetworkDown:
                    case SocketError.NotConnected:
                    case SocketError.Shutdown:
                        _cancelToken.Cancel();
                        OnDisconnect?.Invoke(this);
                        Disconnect();
                        break;

                }
            }
            catch (Exception ex)
            {
                OnPeerException?.Invoke(this, ex);
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public void Disconnect()
        {
            _cancelToken.Cancel();
            _resetEvent.WaitOne();
            try
            {
                _client?.Client?.Shutdown(SocketShutdown.Both);
            }
            catch (Exception ex)
            {
                OnPeerException?.Invoke(this, ex);
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public void Close()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    _cancelToken.Cancel();
                    SpinWait.SpinUntil(() => _pollExited);
                    //_client?.GetStream().Dispose();
                    _client?.Dispose();
                }
                catch (Exception ex)
                {
                    OnPeerException?.Invoke(this, ex);
                }
            });
        }

        private void Maintain(object state)
        {
            if (_client.Connected)
            {
                if (_lastContact < (DateTime.Now.Subtract(QuietTimeout)))
                {
                    OnUnresponsive?.Invoke(this);
                    Disconnect();
                }
                else
                {
                    Send(NetworkQualifier, PingCommand, new byte[0]);
                }
            }
        }

        private async void Poll()
        {
            _pollExited = false;
            _cancelToken = new CancellationTokenSource();
            var headerLength = _serviceIdentifier.Length + 6;
            var timer = new Timer(Maintain, null, TimeSpan.FromSeconds(120), TimeSpan.FromSeconds(120));
            SendIdentify();
            Send(NetworkQualifier, PingCommand, new byte[0]);
            while ((_client.Connected) && (!_cancelToken.IsCancellationRequested))
            {
                try
                {
                    var header = new byte[headerLength];

                    if (Recieve(header) != headerLength)
                        continue;

                    var servIdSegment = new ArraySegment<byte>(header, 0, _serviceIdentifier.Length).ToArray();
                    var lengthSegment = new ArraySegment<byte>(header, _serviceIdentifier.Length, 4).ToArray();
                    var commandSegment = new ArraySegment<byte>(header, _serviceIdentifier.Length + 4, 2).ToArray();

                    if (!servIdSegment.SequenceEqual(_serviceIdentifier))
                    {                         
                        var flushBuffer = new byte[_client.Available];
                        _client.Client.Receive(flushBuffer, _client.Available, SocketFlags.None);
                        continue;
                    }

                    var msgLength = BitConverter.ToInt32(lengthSegment, 0);

                    if (msgLength < 0)
                        continue;

                    if (msgLength > MaxMessageSize)
                        continue;

                    var msgBuffer = new byte[msgLength];

                    var actualRecv = Recieve(msgBuffer);
                    
                    if (actualRecv != msgLength)
                        continue;

                    _lastContact = DateTime.Now;

                    switch (commandSegment[0])
                    {
                        case NetworkQualifier:
                            ProcessNetworkCommand(commandSegment[1], msgBuffer);
                            break;
                        default:
                            var evtTask = Task.Factory.StartNew(() => OnReceiveMessage?.Invoke(this, commandSegment[1], msgBuffer));
                            break;
                    }
                }
                catch (SocketException ex)
                {
                    switch (ex.SocketErrorCode)
                    {
                        case SocketError.ConnectionAborted:
                        case SocketError.ConnectionReset:
                        case SocketError.Disconnecting:
                        case SocketError.HostDown:
                        case SocketError.NetworkDown:
                        case SocketError.NotConnected:
                        case SocketError.Shutdown:
                            _cancelToken.Cancel();
                            OnDisconnect?.Invoke(this);
                            Disconnect();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    OnPeerException?.Invoke(this, ex);
                    await Task.Delay(1000);
                }
            }
            timer.Dispose();
            _pollExited = true;
        }

        private void SendIdentify()
        {
            var data = new Handshake()
            {
                NodeId = _localId,
                Version = _localVersion
            };
            Send(NetworkQualifier, IdentifyCommand, SerializeObject(data));
        }

        private void ProcessNetworkCommand(byte command, byte[] data)
        {
            switch (command)
            {
                case IdentifyCommand:
                    var handshake = DeserializeObject<Handshake>(data);
                    _remoteId = handshake.NodeId;
                    _remoteVersion = handshake.Version;
                    OnIdentify?.Invoke(this);
                    break;
                case PingCommand:
                    break;
            }
        }

        private int Recieve(byte[] msgBuffer)
        {
            var actualRecv = 0;
            while (actualRecv < msgBuffer.Length)
                actualRecv += _client.Client.Receive(msgBuffer, actualRecv, (msgBuffer.Length - actualRecv), SocketFlags.None);

            return actualRecv;
        }

        private static byte[] SerializeObject(object data)
        {
            using (var bw = new MemoryStream())
            {
                var writer = new BsonDataWriter(bw);
                var serializer = new JsonSerializer();
                serializer.TypeNameHandling = TypeNameHandling.Objects;
                serializer.Serialize(writer, data);
                writer.Close();
                bw.TryGetBuffer(out var result);
                return result.Array;
            }
        }

        private static T DeserializeObject<T>(byte[] bson)
        {
            using (var ms = new MemoryStream(bson))
            {
                var bdr = new BsonDataReader(ms);
                var serializer = new JsonSerializer();
                serializer.TypeNameHandling = TypeNameHandling.Objects;
                var result = serializer.Deserialize<T>(bdr);
                bdr.Close();
                return result;
            }
        }
    }

    public delegate void ReceiveMessage(PeerConnection sender, byte command, byte[] data);
    public delegate void PeerEvent(PeerConnection sender);
    public delegate void PeerException(PeerConnection sender, Exception exception);

}
