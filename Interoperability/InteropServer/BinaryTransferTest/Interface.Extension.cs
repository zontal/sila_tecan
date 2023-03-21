using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.Interop.Server.BinaryTransferTest
{
    public partial interface IBinaryTransferTest
    {

        [MetadataType(typeof(string))]
        IRequestInterceptor String { get; }
    }
}
