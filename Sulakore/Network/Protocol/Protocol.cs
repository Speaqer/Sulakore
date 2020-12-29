using System.Buffers;
using System.IO.Pipelines;
using System.Threading;

namespace Sulakore.Network.Protocol
{
    public static class Protocol
    {
        public static ProtocolWriter CreateWriter(this HNode connection)
            => new ProtocolWriter(connection.Transport.Output);

        public static ProtocolWriter CreateWriter(this HNode connection, SemaphoreSlim semaphore)
            => new ProtocolWriter(connection.Transport.Output, semaphore);

        public static ProtocolReader CreateReader(this HNode connection)
            => new ProtocolReader(connection.Transport.Input);

        public static PipeReader CreatePipeReader(this HNode connection, IMessageReader<ReadOnlySequence<byte>> messageReader)
            => new MessagePipeReader(connection.Transport.Input, messageReader);
    }
}
