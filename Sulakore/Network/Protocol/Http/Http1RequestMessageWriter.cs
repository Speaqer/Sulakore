using System;
using System.Buffers;
using System.Net.Http;
using System.Diagnostics;

namespace Sulakore.Network.Protocol.Http
{
    public class Http1RequestMessageWriter : IMessageWriter<HttpRequestMessage>
    {
        private static ReadOnlySpan<byte> Http11 => new byte[] { (byte)'H', (byte)'T', (byte)'T', (byte)'P', (byte)'/', (byte)'1', (byte)'.', (byte)'1' };
        private static ReadOnlySpan<byte> NewLine => new byte[] { (byte)'\r', (byte)'\n' };
        private static ReadOnlySpan<byte> Space => new byte[] { (byte)' ' };
        private static ReadOnlySpan<byte> Colon => new byte[] { (byte)':' };
        
        public Http1RequestMessageWriter()
        { }

        public void WriteMessage(HttpRequestMessage message, IBufferWriter<byte> output)
        {
            Debug.Assert(message.Method != null);
            Debug.Assert(message.RequestUri != null);

            var writer = new BufferWriter<IBufferWriter<byte>>(output);
            writer.WriteAscii(message.Method.Method);
            writer.Write(Space);
            writer.WriteAscii(message.RequestUri.ToString());
            writer.Write(Space);
            writer.Write(Http11);
            writer.Write(NewLine);

            foreach (var header in message.Headers)
            {
                foreach (var value in header.Value)
                {
                    writer.WriteAscii(header.Key);
                    writer.Write(Colon);
                    writer.Write(Space);
                    writer.WriteAscii(value);
                    writer.Write(NewLine);
                }
            }

            if (message.Content != null)
            {
                foreach (var header in message.Content.Headers)
                {
                    foreach (var value in header.Value)
                    {
                        writer.WriteAscii(header.Key);
                        writer.Write(Colon);
                        writer.Write(Space);
                        writer.WriteAscii(value);
                        writer.Write(NewLine);
                    }
                }
            }

            writer.Write(NewLine);
            writer.Commit();
        }
    }
}
