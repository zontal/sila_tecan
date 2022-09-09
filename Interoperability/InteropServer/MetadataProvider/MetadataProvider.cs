using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.Interop.Server.MetadataProvider
{
    [Export( typeof( IMetadataProviderTest ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class MetadataProvider : IMetadataProviderTest
    {
        public IRequestInterceptor StringMetadata { get; } = new StringMetadataProvider();

        public IRequestInterceptor TwoIntegersMetadata { get; } = new TwoIntegersMetadataProvider();
    }
}
