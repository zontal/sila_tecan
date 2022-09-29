using System;
using System.Collections.Generic;
using System.Text;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.Interop.Server.MetadataProvider
{
    public partial interface IMetadataProvider
    {
        [MetadataType( typeof( string ) )]
        IRequestInterceptor StringMetadata { get; }

        [MetadataType( typeof( TwoIntegersMetadata ) )]
        IRequestInterceptor TwoIntegersMetadata { get; }
    }
}
