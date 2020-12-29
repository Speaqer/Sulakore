using System;
using System.Net.Sockets;
using System.IO.Pipelines;

namespace Sulakore.Network.Internal
{
    internal class SocketReceiver : IDisposable
    {
        private readonly Socket _socket;
        private readonly SocketAwaitableEventArgs _awaitableEventArgs;
        
        public SocketReceiver(Socket socket, PipeScheduler scheduler)
        {
            _socket = socket;
            _awaitableEventArgs = new SocketAwaitableEventArgs(scheduler);
        }

        public SocketAwaitableEventArgs ReceiveAsync(Memory<byte> buffer)
        {
            _awaitableEventArgs.SetBuffer(buffer);

            if (!_socket.ReceiveAsync(_awaitableEventArgs))
            {
                _awaitableEventArgs.Complete();
            }

            return _awaitableEventArgs;
        }

        public void Dispose()
        {
            _awaitableEventArgs.Dispose();
        }
    }
}
