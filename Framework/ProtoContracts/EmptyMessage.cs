using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using ProtoBuf;
using Tecan.Sila2.Binary;

namespace Tecan.Sila2
{

    /// <summary>
    /// The data transfer object of the response to delete a binary
    /// </summary>
    [ProtoContract()]
    public partial class EmptyMessage
    {
    }
}
