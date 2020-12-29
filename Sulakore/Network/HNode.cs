using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.IO.Pipelines;
using System.Threading.Tasks;

using Sulakore.Network.Internal;

namespace Sulakore.Network
{
    /// <summary>
    /// Represents an connection to an endpoint. This connection will keep pumping <see cref="Transport"/> input pipe and reading from Transport Output to socket
    /// </summary>
    public class HNode : IAsyncDisposable
    {
        private volatile bool _aborted;

        private readonly Socket _socket;
        private readonly SocketSender _sender;
        private readonly SocketReceiver _receiver;

        private readonly IDuplexPipe _application;

        public IDuplexPipe Transport { get; set; }
        public HotelEndPoint RemoteEndPoint { get; private set; }

        //TODO: Provice cts here? I think pipe completions are enough though.
        //TODO: duplex configurations
        public HNode(Socket socket)
        {
            socket.NoDelay = true;
            socket.LingerState = new LingerOption(false, 0);

            if (socket.RemoteEndPoint is IPEndPoint ipEndPoint)
            {
                RemoteEndPoint = new HotelEndPoint(ipEndPoint);
            }

            _socket = socket;
            _sender = new SocketSender(socket, PipeScheduler.ThreadPool);
            _receiver = new SocketReceiver(socket, PipeScheduler.ThreadPool);

            (Transport, _application) = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
        }

        /// <summary>
        /// Starts receiving and sending data. //TODO: Accurate documentation on all of the new stuff.
        /// </summary>
        public async Task StartAsync()
        {
            try
            {
                // Spawn send and receive logic
                var receiveTask = ReceiveAsync();
                var sendTask = SendAsync();

                // Now wait for both to complete
                await receiveTask;
                await sendTask;

                _receiver.Dispose();
                _sender.Dispose();

                _socket.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected exception in {nameof(HNode)}.{nameof(StartAsync)}.");
                await _application.Input.CompleteAsync(ex);
            }

            // Complete the output after disposing the socket
            await _application.Input.CompleteAsync();
        }

        private async Task ReceiveAsync()
        {
            Exception error = null;

            try
            {
                await ProcessReceivesAsync().ConfigureAwait(false);
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset)
            {
                error = new Exception(ex.Message, ex);
            }
            catch (SocketException ex) when (ex.SocketErrorCode is SocketError.OperationAborted
                or SocketError.ConnectionAborted or SocketError.Interrupted or SocketError.InvalidArgument)
            {
                if (!_aborted)
                {
                    // Calling Dispose after ReceiveAsync can cause an "InvalidArgument" error on *nix.
                    error = new Exception(ex.Message);
                }
            }
            catch (ObjectDisposedException)
            {
                if (!_aborted)
                {
                    error = new Exception("Disposed");
                }
            }
            catch (IOException ex)
            {
                error = ex;
            }
            catch (Exception ex)
            {
                error = new IOException(ex.Message, ex);
            }
            finally
            {
                if (_aborted)
                {
                    error ??= new Exception("Successful abort");
                }

                await _application.Output.CompleteAsync(error).ConfigureAwait(false);
            }
        }
        private async Task ProcessReceivesAsync()
        {
            while (true)
            {
                //TODO: _waitForData worth it for our use case?
                //TODO: .TryRead optimization? source @mgravell

                // Ensure we have some reasonable amount of buffer space
                var buffer = _application.Output.GetMemory();

                int bytesReceived = await _receiver.ReceiveAsync(buffer);
                if (bytesReceived == 0) break;

                _application.Output.Advance(bytesReceived);

                var flushTask = _application.Output.FlushAsync();

                FlushResult result;
                if (flushTask.IsCompletedSuccessfully)
                {
                    result = flushTask.Result;
                }
                else result = await flushTask.ConfigureAwait(false);

                //TODO: Canceled?
                if (result.IsCompleted)
                {
                    // Pipe consumer is shut down, do we stop writing
                    break;
                }
            }
        }

        private async Task<Exception> SendAsync()
        {
            Exception error = null;

            try
            {
                await ProcessSendsAsync().ConfigureAwait(false);
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted)
            {
                error = null;
            }
            catch (ObjectDisposedException)
            {
                error = null;
            }
            catch (IOException ex)
            {
                error = ex;
            }
            catch (Exception ex)
            {
                error = new IOException(ex.Message, ex);
            }
            finally
            {
                _aborted = true;
                _socket.Shutdown(SocketShutdown.Both);
            }

            return error;
        }
        private async Task ProcessSendsAsync()
        {
            while (true)
            {
                // Wait for data to write from the pipe producer
                var result = await _application.Input.ReadAsync().ConfigureAwait(false);
                var buffer = result.Buffer;

                if (result.IsCanceled)
                {
                    break;
                }

                var end = buffer.End;
                var isCompleted = result.IsCompleted;
                if (!buffer.IsEmpty)
                {
                    await _sender.SendAsync(buffer);
                }

                _application.Input.AdvanceTo(end);

                if (isCompleted)
                {
                    break;
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (Transport != null)
            {
                await Transport.Output.CompleteAsync().ConfigureAwait(false);
                await Transport.Input.CompleteAsync().ConfigureAwait(false);
            }

            // Completing these loops will cause ExecuteAsync to Dispose the socket.
        }

        public static async Task<HNode> ConnectAsync(IPEndPoint endpoint)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(endpoint).ConfigureAwait(false);
            }
            catch { /* Ignore all exceptions. */ }

            if (!socket.Connected)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                return null;
            }
            else
            {
                var node = new HNode(socket);
                if (endpoint is HotelEndPoint hotelEndPoint)
                {
                    node.RemoteEndPoint = hotelEndPoint;
                }
                return node;
            }
        }
    }
}
