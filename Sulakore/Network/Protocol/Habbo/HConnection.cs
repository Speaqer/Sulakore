using System;
using System.Net.Security;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;

using Sulakore.Network.Internal;
using Sulakore.Network.Protocol.Http;
using Sulakore.Network.Protocol.WebSockets;

namespace Sulakore.Network.Protocol.Habbo
{
    public class HConnection
    {
        private readonly HNode _node;
        
        private HProtocol<HFormat, HFormat> _protocol;
        private WebSocketProtocol _wsProtocol;

        public HFormat SendFormat { get; set; }
        public HFormat ReceiveFormat { get; set; }

        public WebSocketProtocolType WebSocketType { get; set; }

        public HotelEndPoint RemoteEndPoint => _node.RemoteEndPoint;

        public HConnection(HNode node)
        {
            _node = node;
        }

        //public async Task<bool> DetermineProtocolAsync() { } // Just look at 6 bytes from the transport pipe, don't advance

        // "OK"
        private static ReadOnlySpan<byte> OkBytes
            => new byte[] { (byte)'O', (byte)'K' };

        // "StartTLS"
        private static ReadOnlySpan<byte> StartTLSBytes
            => new byte[] { (byte)'S', (byte)'t', (byte)'a', (byte)'r', (byte)'t', (byte)'T', (byte)'L', (byte)'S' };


        //TODO: Could we just create better transport abstraction layer over hnode?
        public async Task<bool> UpgradeWebSocketAsync(WebSocketProtocolType type, bool secure = true)
        {
            //TODO: type, secure args

            var secureTransport = new DuplexPipeStreamAdapter<SslStream>(_node.Transport,
                stream => new SslStream(stream, leaveInnerStreamOpen: false, ValidateRemoteCertificate));

            SslStream secureTransportStream = secureTransport.Stream;
            
            await secureTransportStream.AuthenticateAsClientAsync(RemoteEndPoint.Host, null, false).ConfigureAwait(false);
            if (!secureTransportStream.IsAuthenticated) return false;

            //Finally do the actual upgrade
            var upgradeProtocol = new HttpUpgradeProtocol(secureTransport);
            var response = await upgradeProtocol.UpgradeToWebSocketAsync().ConfigureAwait(false);

            _wsProtocol = new WebSocketProtocol(secureTransport, type);
            
            //TODO: Write StartTLSBytes
            //TODO: Rcv && seq equal

            // Initialize the second secure tunnel layer where ONLY the WebSocket payload data will be read/written from/to.
            
            
            //_securePayloadLayer = new SslStream(this, true, ValidateRemoteCertificate);
            //await _securePayloadLayer.AuthenticateAsClientAsync(RemoteEndPoint.Host, null, false).ConfigureAwait(false);
            //
            //return IsUpgraded;
            return true;
        }

        private static bool ValidateRemoteCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => true;


        //public static HConnection Create(HNodeNew node) {}
    }
}
