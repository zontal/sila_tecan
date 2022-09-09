using System;
using System.Collections.Generic;
using System.Text;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.Interop.Server.MetadataProvider
{
    internal class StringMetadataProvider : StringMetadataInterceptorBase
    {
        [ThreadStatic]
        private static string _value;

        public static string Value => _value;

        public override bool AppliesToCommands => true;

        public override bool AppliesToProperties => false;

        public override int Priority => 0;

        public override IRequestInterception Intercept( string commandIdentifier, string stringMetadata )
        {
            _value = stringMetadata;
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
