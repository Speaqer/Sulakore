using Sulakore.Cryptography.Ciphers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sulakore.Network.Protocol.Habbo
{
    public class HPacketWriter : IMessageWriter<HPacketNew>
    {
        public IStreamCipher Encrypter { get; set; }

        public HPacketWriter()
        {

        }

        public void WriteMessage(HPacketNew message, IBufferWriter<byte> output) => message.WriteTo(output);
    }
}
