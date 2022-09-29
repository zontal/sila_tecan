using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.Interop.Server.MetadataProvider
{
    [Export( typeof( IMetadataProvider ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class MetadataProvider : IMetadataProvider
    {
        private readonly StringMetadataProvider _stringMetadataProvider;
        private readonly TwoIntegersMetadataProvider _twoIntegersProvider;

        [ImportingConstructor]
        public MetadataProvider( StringMetadataProvider stringMetadataProvider, TwoIntegersMetadataProvider twoIntegersProvider )
        {
            _stringMetadataProvider = stringMetadataProvider;
            _twoIntegersProvider = twoIntegersProvider;
        }

        public IRequestInterceptor StringMetadata => _stringMetadataProvider;

        public IRequestInterceptor TwoIntegersMetadata => _twoIntegersProvider;
    }
}
