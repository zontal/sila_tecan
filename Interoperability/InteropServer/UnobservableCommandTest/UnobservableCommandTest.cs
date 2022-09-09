using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;

namespace Tecan.Sila2.Interop.Server.UnobservableCommandTest
{
    [Export( typeof( IUnobservableCommandTest ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class UnobservableCommandTest : IUnobservableCommandTest
    {
        public void CommandWithoutParametersAndResponses()
        {
        }

        [return: SilaIdentifier( "StringRepresentation" )]
        public string ConvertIntegerToString( long integer )
        {
            return Convert.ToString( integer );
        }

        [return: SilaDisplayName( "Joined parameters" ), SilaIdentifier( "JoinedParameters" )]
        public string JoinIntegerAndString( long integer, string @string )
        {
            return Convert.ToString( integer ) + @string;
        }

        [return: InlineStruct]
        public SplitStringAfterFirstCharacterResponse SplitStringAfterFirstCharacter( string @string )
        {
            if(string.IsNullOrEmpty( @string ))
            {
                return new SplitStringAfterFirstCharacterResponse( string.Empty, string.Empty );
            }
            return new SplitStringAfterFirstCharacterResponse( @string[0].ToString(), @string.Substring( 1 ) );
        }
    }
}
