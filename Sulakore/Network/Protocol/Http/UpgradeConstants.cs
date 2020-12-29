using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sulakore.Network.Protocol.Http
{
    public static class UpgradeConstants
    {
        #region Constants
        
        // "Sec-WebSocket-Key: "
        private static ReadOnlySpan<byte> SecWebSocketKeyBytes
            => new byte[] { (byte)'S', (byte)'e', (byte)'c', (byte)'-',
                (byte)'W', (byte)'e', (byte)'b', (byte)'S', (byte)'o', (byte)'c', (byte)'k', (byte)'e', (byte)'t', (byte)'-',
                (byte)'K', (byte)'e', (byte)'y', (byte)':', (byte)' ' };

        // "HTTP/1.1 101 Switching Protocols\r\nConnection: Upgrade\r\nUpgrade: websocket\r\nSec-WebSocket-Accept: "
        private static ReadOnlySpan<byte> UpgradeWebSocketResponseBytes
            => new byte[] { (byte)'H', (byte)'T', (byte)'T', (byte)'P', (byte)'/', (byte)'1', (byte)'.', (byte)'1', (byte)' ', (byte)'1', (byte)'0', (byte)'1', (byte)' ',
            (byte)'S', (byte)'w', (byte)'i', (byte)'t', (byte)'c', (byte)'h', (byte)'i', (byte)'n', (byte)'g', (byte)' ', (byte)'P', (byte)'r', (byte)'o', (byte)'t', (byte)'o', (byte)'c', (byte)'o', (byte)'l', (byte)'s', 13, 10,
            (byte)'C', (byte)'o', (byte)'n', (byte)'n', (byte)'e', (byte)'c', (byte)'t', (byte)'i', (byte)'o', (byte)'n', (byte)':', (byte)' ', (byte)'U', (byte)'p', (byte)'g', (byte)'r', (byte)'a', (byte)'d', (byte)'e', 13, 10,
            (byte)'S', (byte)'e', (byte)'c', (byte)'-', (byte)'W', (byte)'e', (byte)'b', (byte)'S', (byte)'o', (byte)'c', (byte)'k', (byte)'e', (byte)'t', (byte)'-', (byte)'A', (byte)'c', (byte)'c', (byte)'e', (byte)'p', (byte)'t', (byte)':', (byte)' ' };
        #endregion
    }
}
