using System;
using System.Buffers;
using System.Threading;
using System.IO.Pipelines;
using System.Threading.Tasks;

using Sulakore.Cryptography.Ciphers;

namespace Sulakore.Network.Protocol.Habbo
{
    //This will ONLY handle the HABBO protocol! No websocket upgrades or TLS stuff should reach here!
    public class HProtocol<TInFormat, TOutFormat> : IMessageWriter<HPacket>, IMessageReader<HPacket>
        where TInFormat : HFormat
        where TOutFormat : HFormat
    {
        public IStreamCipher Encrypter { get; set; }
        public IStreamCipher Decrypter { get; set; }

        private readonly ProtocolReader _protocolReader;
        private readonly ProtocolWriter _protocolWriter;

        public HProtocol(IDuplexPipe transport)
        {
            _protocolReader = new ProtocolReader(transport.Input);
            _protocolWriter = new ProtocolWriter(transport.Output);
        }

        public async ValueTask<HPacket> ReadAsync(CancellationToken cancellationToken = default)
        {
            var result = await _protocolReader.ReadAsync(this, cancellationToken).ConfigureAwait(false);
            var message = result.Message;

            _protocolReader.Advance();
            return message;
        }
        public ValueTask WriteAsync(HPacket packet, CancellationToken cancellationToken = default)
        {
            return _protocolWriter.WriteAsync(this, packet, cancellationToken);
        }

        public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out HPacket message)
        {
            throw new NotImplementedException();
        }
        public void WriteMessage(HPacket message, IBufferWriter<byte> output)
        {
            throw new NotImplementedException();
        }
    }
}
