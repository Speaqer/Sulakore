using System;
using System.Net;
using System.Text;
using System.Buffers;
using System.Net.Http;
using System.Buffers.Text;

namespace Sulakore.Network.Protocol.Http
{
    public class Http1ResponseMessageReader : IMessageReader<HttpResponseMessage>
    {
        private static ReadOnlySpan<byte> NewLine => new byte[] { (byte)'\r', (byte)'\n' };
        private static ReadOnlySpan<byte> TrimChars => new byte[] { (byte)' ', (byte)'\t' };

        private HttpResponseMessage _httpResponseMessage = new HttpResponseMessage();

        private State _state;

        public Http1ResponseMessageReader()
        { }

        public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out HttpResponseMessage message)
        {
            string debug = Encoding.UTF8.GetString(input);

            var sequenceReader = new SequenceReader<byte>(input);
            message = null;

            switch (_state)
            {
                case State.StartLine:
                    if (!sequenceReader.TryReadTo(out ReadOnlySpan<byte> version, (byte)' '))
                    {
                        return false;
                    }

                    if (!sequenceReader.TryReadTo(out ReadOnlySpan<byte> statusCodeText, (byte)' '))
                    {
                        return false;
                    }

                    if (!sequenceReader.TryReadTo(out ReadOnlySpan<byte> statusText, NewLine))
                    {
                        return false;
                    }

                    Utf8Parser.TryParse(statusCodeText, out int statusCode, out _);

                    _httpResponseMessage.StatusCode = (HttpStatusCode)statusCode;
                    var reasonPhrase = Encoding.ASCII.GetString(statusText);
                    _httpResponseMessage.ReasonPhrase = reasonPhrase;
                    _httpResponseMessage.Version = new Version(1, 1); // TODO: Check

                    _state = State.Headers;

                    consumed = sequenceReader.Position;

                    goto case State.Headers;

                case State.Headers:
                    while (sequenceReader.TryReadTo(out ReadOnlySequence<byte> headerLine, NewLine))
                    {
                        if (headerLine.Length == 0)
                        {
                            consumed = sequenceReader.Position;
                            examined = consumed;

                            message = _httpResponseMessage;

                            // End of headers
                            _state = State.Body;
                            break;
                        }

                        // Parse the header
                        Http1RequestMessageReader.ParseHeader(headerLine, out var headerName, out var headerValue);

                        var key = Encoding.ASCII.GetString(headerName.Trim(TrimChars));
                        var value = Encoding.ASCII.GetString(headerValue.Trim(TrimChars));

                        if (!_httpResponseMessage.Headers.TryAddWithoutValidation(key, value))
                        {
                            System.Diagnostics.Debugger.Break();
                        }

                        consumed = sequenceReader.Position;
                    }
                    break;
                default:
                    break;
            }

            return _state == State.Body;
        }

        private enum State
        {
            StartLine,
            Headers,
            Body
        }
    }
}
