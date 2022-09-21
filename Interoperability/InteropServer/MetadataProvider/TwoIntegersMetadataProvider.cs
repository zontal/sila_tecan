using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using Tecan.Sila2.Interop.Server.MetadataConsumerTest;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.Interop.Server.MetadataProvider
{
    [Export( typeof( IRequestInterceptor ) )]
    [Export( typeof( TwoIntegersMetadataProvider ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class TwoIntegersMetadataProvider : TwoIntegersMetadataInterceptorBase
    {
        [ThreadStatic]
        private static TwoIntegersMetadata _value;

        public static TwoIntegersMetadata Value => _value;

        public override bool AppliesToCommands => true;

        public override bool AppliesToProperties => false;

        public override int Priority => 0;

        public override IRequestInterception Intercept( string commandIdentifier, TwoIntegersMetadata twoIntegersMetadata )
        {
            _value = twoIntegersMetadata;
            return null;
        }

        public override bool IsInterceptRequired( Feature feature, string commandIdentifier )
        {
            if(feature.FullyQualifiedIdentifier != "org.silastandard/test/MetadataConsumerTest/v1")
            {
                return false;
            }
            return commandIdentifier.Contains( nameof( IMetadataConsumerTest.UnpackMetadata ) );
        }
    }
}
