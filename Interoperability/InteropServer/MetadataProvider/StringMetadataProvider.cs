using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Threading;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.Interop.Server.MetadataProvider
{
    [Export( typeof( IRequestInterceptor ) )]
    [Export( typeof( StringMetadataProvider ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class StringMetadataProvider : StringMetadataInterceptorBase
    {
        private static AsyncLocal<string> _value = new AsyncLocal<string>();

        public static string Value { get => _value.Value; set => _value.Value = value; }

        public override bool AppliesToCommands => true;

        public override bool AppliesToProperties => true;

        public override int Priority => 0;

        public override IRequestInterception Intercept( string commandIdentifier, string stringMetadata )
        {
            _value.Value = stringMetadata;
            return null;
        }

        public override bool IsInterceptRequired( Feature feature, string commandIdentifier )
        {
            if(feature.FullyQualifiedIdentifier != "org.silastandard/test/MetadataConsumerTest/v1")
            {
                return false;
            }
            return true;
        }
    }
}
