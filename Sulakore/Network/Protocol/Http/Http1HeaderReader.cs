using System;
using System.Buffers;

namespace Sulakore.Network.Protocol.Http
{
    //TODO: Can create fast-path for span read
    internal class Http1HeaderReader : IMessageReader<Http1Header>
    {
        private const byte SP = (byte)' ';
        private const byte HT = (byte)'\t';
        private const byte COLON = (byte)':';
        private const byte CR = (byte)'\r';
        private const byte LF = (byte)'\n';

        private static ReadOnlySpan<byte> ColonOrWhitespace => new byte[] { COLON, CR, SP, HT };
        private static ReadOnlySpan<byte> SpOrHt => new byte[] { SP, HT };
        private static ReadOnlySpan<byte> CrOrLf => new byte[] { CR, LF };

        public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out Http1Header message)
        {
            message = default;
            var reader = new SequenceReader<byte>(input);
            if (!reader.TryReadToAny(out ReadOnlySequence<byte> fieldName, ColonOrWhitespace, advancePastDelimiter: false))
            {
                return false;
            }

            reader.TryRead(out var delimiter);
            if (delimiter != COLON || fieldName.IsEmpty)
            {
                examined = reader.Position;
                return true;
            }

            reader.AdvancePastAny(SpOrHt);

            if (!reader.TryReadToAny(out ReadOnlySequence<byte> fieldValue, CrOrLf, advancePastDelimiter: false))
            {
                return false;
            }

            reader.TryRead(out delimiter);
            if (delimiter != CR)
            {
                examined = reader.Position;
                return true;
            }

            if (!reader.TryRead(out var final))
            {
                return false;
            }

            if (final != LF)
            {
                examined = reader.Position;
                return true;
            }

            consumed = examined = reader.Position;

            ReadOnlyMemory<byte> fieldValueMemory = fieldValue.ToMemory();
            var fieldValueSpan = fieldValueMemory.Span;
            var i = fieldValueSpan.Length - 1;
            for (; i >= 0; i--)
            {
                var b = fieldValueSpan[i];
                if (b == SP || b == HT)
                {
                    continue;
                }
                break;
            }
            message = new Http1Header(fieldName.ToMemory(), fieldValueMemory.Slice(0, i + 1));
            return true;
        }
    }
}
