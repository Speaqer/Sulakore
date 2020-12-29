using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sulakore.Network.Protocol.Habbo
{
    /// <summary>
    /// A protocol abstraction for comunicating with Habbo endpoints.
    /// </summary>
    public interface IHProtocol
    {
        HFormat SendFormat { get; set; }
        HFormat ReceiveFormat { get; set; }
    }
}
