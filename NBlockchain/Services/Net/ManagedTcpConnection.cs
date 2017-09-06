using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBlockchain.Services.Net
{
    public class ManagedTcpConnection
    {
        private readonly byte[] _serviceIdentifier;
        private readonly TcpClient _client;
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(true);

        public event ReceiveMessage OnReceiveMessage;
        public event Disconnect OnDisconnect;

        public EndPoint RemoteEndPoint => _client.Client.RemoteEndPoint;

        public ManagedTcpConnection(byte[] serviceIdentifier)
        {
            _client = new TcpClient();
            _serviceIdentifier = serviceIdentifier;
        }

        public ManagedTcpConnection(TcpClient client, byte[] serviceIdentifier)
        {
            _client = client;
            _serviceIdentifier = serviceIdentifier;
            Task.Factory.StartNew(Poll);
        }

        public void Connect(string host, int port)
        {
            _client.Connect(host, port);
            Task.Factory.StartNew(Poll);
        }

        public void Send(byte command, byte[] data)
        {
            _resetEvent.WaitOne();
            try
            {
                _client.Client.SendTimeout = 1000;
                var headerLength = _serviceIdentifier.Length + 5;
                var lenBuffer = BitConverter.GetBytes(data.Length);
                var message = new byte[headerLength + data.Length];
                _serviceIdentifier.CopyTo(message, 0);
                lenBuffer.CopyTo(message, _serviceIdentifier.Length);
                message[_serviceIdentifier.Length + 4] = command;
                data.CopyTo(message, _serviceIdentifier.Length + 5);
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
                        OnDisconnect?.Invoke(this);
                        break;

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SEND ERR: {ex.Message}");
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public void Disconnect()
        {
            _client.Client.Disconnect(false);
            _client.Close();
        }

        private async void Poll()
        {
            //_client.ReceiveTimeout = 3000;
            var headerLength = _serviceIdentifier.Length + 5;
            while (_client.Connected)
            {
                try
                {
                    var header = new byte[headerLength];

                    if (_client.Client.Receive(header) != headerLength)
                        continue;

                    var servIdSegment = new ArraySegment<byte>(header, 0, _serviceIdentifier.Length);
                    var lengthSegment = new ArraySegment<byte>(header, _serviceIdentifier.Length, 4);
                    var commandSegment = new ArraySegment<byte>(header, _serviceIdentifier.Length + 4, 1);

                    var msgLength = BitConverter.ToInt32(lengthSegment.ToArray(), 0);
                    var msgBuffer = new byte[msgLength];

                    if (!servIdSegment.ToArray().SequenceEqual(_serviceIdentifier))
                        continue;

                    if (_client.Client.Receive(msgBuffer) != msgLength)
                        continue;


                    var evtTask = Task.Factory.StartNew(() => OnReceiveMessage?.Invoke(this, commandSegment.ToArray()[0], msgBuffer));
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
                            OnDisconnect?.Invoke(this);
                            return;
                    }
                }
                catch (Exception ex)
                {
                    //log
                    Console.WriteLine($"EXCEPTION: {ex.Message}");
                    await Task.Delay(1000);
                }
            }
        }
    }

    public delegate void ReceiveMessage(ManagedTcpConnection sender, byte command, byte[] data);
    public delegate void Disconnect(ManagedTcpConnection sender);

}
