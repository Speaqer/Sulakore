using System;
using System.Text;
using System.Net.Http;
using System.Threading;
using System.Buffers.Text;
using System.IO.Pipelines;
using System.Threading.Tasks;

using Sulakore.Habbo.Web;

namespace Sulakore.Network.Protocol.Http
{
    public class HttpUpgradeProtocol
    {
        private static readonly Random _rng = new Random();

        private readonly HNode _node;
        
        private readonly ProtocolReader _protocolReader;
        private readonly ProtocolWriter _protocolWriter;
        
        private readonly IMessageReader<HttpRequestMessage> _requestReader;
        private readonly IMessageReader<HttpResponseMessage> _responseReader; 

        private readonly IMessageWriter<HttpRequestMessage> _requestWriter;

        private readonly IDuplexPipe _transport;

        public HttpUpgradeProtocol(IDuplexPipe transport)
        {
            _transport = transport;

            _protocolReader = new ProtocolReader(transport.Input);
            _protocolWriter = new ProtocolWriter(transport.Output);

            _requestReader = new Http1RequestMessageReader();
            _responseReader = new Http1ResponseMessageReader();

            _requestWriter = new Http1RequestMessageWriter();
        }

        public ValueTask<FlushResult> WriteUpgradeRequestAsync()
        {
            static string GenerateWebSocketKey()
            {
                Span<byte> keyGenerationBuffer = stackalloc byte[24];
                _rng.NextBytes(keyGenerationBuffer.Slice(0, 16));

                Base64.EncodeToUtf8InPlace(keyGenerationBuffer, 16, out int encodedSize);
                return Encoding.UTF8.GetString(keyGenerationBuffer.Slice(0, encodedSize));
            }

            //TODO: Recycle and other crazy ass optimizations
            HttpRequestMessage upgradeMessage = new(HttpMethod.Get, "/websocket");
            upgradeMessage.Headers.Add("Host", _node.RemoteEndPoint.Host);
            upgradeMessage.Headers.Add("Connection", "Upgrade");
            upgradeMessage.Headers.Add("User-Agent", HAPI.CHROME_USER_AGENT);
            upgradeMessage.Headers.Add("Sec-WebSocket-Version", "13");
            upgradeMessage.Headers.Add("Sec-WebSocket-Key", GenerateWebSocketKey());

            _requestWriter.WriteMessage(upgradeMessage, _transport.Output);
            return _transport.Output.FlushAsync();
        }

        public async Task<HttpResponseMessage> UpgradeToWebSocketAsync(CancellationToken cancellationToken = default)
        {
            var writeResult = await WriteUpgradeRequestAsync();
            return await ReadResponseAsync();
        }

        public async ValueTask<HttpRequestMessage> ReadRequestAsync(CancellationToken cancellationToken = default)
        {
            var result = await _protocolReader.ReadAsync(_requestReader, cancellationToken).ConfigureAwait(false);
            var message = result.Message;

            _protocolReader.Advance();
            return message;
        }
        public async ValueTask<HttpResponseMessage> ReadResponseAsync(CancellationToken cancellationToken = default)
        {
            var result = await _protocolReader.ReadAsync(_responseReader, cancellationToken).ConfigureAwait(false);
            var message = result.Message;

            _protocolReader.Advance();
            return message;
        }
    }
}
