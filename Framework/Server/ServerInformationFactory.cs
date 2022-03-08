using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// A helper class to obtain the server information from assembly attributes
    /// </summary>
    public static class ServerInformationFactory
    {
        /// <summary>
        /// Gets the server information for the provided assembly
        /// </summary>
        /// <returns>An object that contains the server information for this server</returns>
        public static ServerInformation GetServerInformation()
        {
            return GetServerInformation( Assembly.GetEntryAssembly() );
        }

        /// <summary>
        /// Gets the server information for the provided assembly
        /// </summary>
        /// <returns>An object that contains the server information for this server</returns>
        public static ServerInformation GetServerInformation( Assembly entryAssembly )
        {
            if(entryAssembly == null)
            {
                throw new ArgumentNullException( nameof( entryAssembly ) );
            }

            var entryAssemblyName = entryAssembly.GetName();
            var serverType = entryAssembly.GetCustomAttribute<ServerTypeAttribute>();
            var product = entryAssembly.GetCustomAttribute<AssemblyProductAttribute>();
            return new ServerInformation(
                CreateServerTypeString( serverType?.ServerType ?? product?.Product ?? entryAssemblyName.Name ),
                entryAssembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description,
                entryAssembly.GetCustomAttribute<VendorUriAttribute>()?.VendorUri,
                entryAssemblyName.Version.ToString( 2 ) );
        }

        private static string CreateServerTypeString( string input )
        {
            var sb = new StringBuilder();
            foreach(var character in input)
            {
                if((character >= 'a' && character <= 'z') ||
                   (character >= 'A' && character <= 'Z') ||
                   char.IsDigit( character ) || character == '.')
                {
                    sb.Append( character );
                }
                else if(character == 'ä')
                {
                    sb.Append( "ae" );
                }
                else if(character == 'ö')
                {
                    sb.Append( "oe" );
                }
                else if(character == 'ü')
                {
                    sb.Append( "ue" );
                }
            }

            return sb.ToString();
        }
    }
}
