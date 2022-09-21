using System;
using System.CodeDom;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using CommandLine;
using Tecan.Sila2.Generator.Contracts;
using Tecan.Sila2.Generator.Helper;

namespace Tecan.Sila2.Generator.CommandLine
{
    /// <summary>
    /// Describes the command to generate a provider
    /// </summary>
    [Export( typeof( ICommandLineVerb ) )]
    [PartCreationPolicy( CreationPolicy.NonShared )]
    [Verb( "generate-provider", HelpText = "Generates the provider for a given SiLA feature" )]
    internal class GenerateProviderVerb : VerbBase
    {
        /// <summary>
        /// The path to the feature from which the provider shall be generated
        /// </summary>
        [Value( 0, Required = true, HelpText = "The path to the feature from which the provider shall be generated" )]
        public string FeaturePath { get; set; }

        /// <summary>
        /// Additional namespaces the provider needs to import, separated by semicolon (;)
        /// </summary>
        [Option( "import-namespaces", HelpText = "Additional namespaces the provider needs to import, separated by semicolon (;)", Required = false )]
        public string ImportedNamespaces { get; set; }

        /// <summary>
        /// The namespace for the newly generated provider. Defaults to a combination of originator and use case if omitted.
        /// </summary>
        [Option( 'n', "namespace", HelpText = "The namespace for the newly generated provider. Defaults to a combination of originator and use case if omitted.", Required = false )]
        public string Namespace { get; set; }

        /// <summary>
        /// The path where the DTOs should be generated to
        /// </summary>
        [Value( 1, Required = true, HelpText = "The path where the DTOs should be generated to" )]
        public string DtoPath { get; set; }

        /// <summary>
        /// The path where the provider should be generated to. If no provider shall be generated, path to the client
        /// </summary>
        [Value( 2, Required = true, HelpText = "The path where the provider should be generated to. If no provider shall be generated, path to the client" )]
        public string ProviderPath { get; set; }

        /// <summary>
        /// The path to the client in case both server and client are generated
        /// </summary>
        [Value( 3, Required = false, HelpText = "The path to the client in case both server and client are generated" )]
        public string ClientPath { get; set; }

        /// <summary>
        /// If set, only the client code will be generated
        /// </summary>
        [Option( 'c', "client-only", HelpText = "If set, only the client code will be generated",
            Required = false )]
        public bool ClientOnly
        {
            get => GenerateClient && !GenerateServer;
            set => (GenerateClient, GenerateServer) = (true, !value);
        }

        /// <summary>
        /// If set, only the server code will be generated
        /// </summary>
        [Option( 's', "server-only", HelpText = "If set, only the server code will be generated", Required = false )]
        public bool ServerOnly
        {
            get => GenerateServer && !GenerateClient;
            set => (GenerateClient, GenerateServer) = (!value, true);
        }

        /// <summary>
        /// Gets a value indicating whether the client should be generated
        /// </summary>
        public bool GenerateClient { get; private set; } = true;

        /// <summary>
        /// Gets a value indicating whether the server should be generated
        /// </summary>
        public bool GenerateServer { get; private set; } = true;

        public override void Execute( CompositionContainer container )
        {
            var channel = SetupLogging( container );
            channel.Info( "Loading feature" );
            var feature = FeatureSerializer.Load( FeaturePath );
            var ns = Namespace ?? feature.Namespace;

            var dtoGenerator = container.GetExportedValue<IDtoGenerator>();
            var providerGenerator = container.GetExportedValue<IServerGenerator>();
            var clientGenerator = container.GetExportedValue<IClientGenerator>();

            var dtoUnit = dtoGenerator.GenerateInterfaceUnit( feature, ns );
            var providerUnit = GenerateServer ? providerGenerator.GenerateServer( feature, ns ) : null;
            var clientUnit = GenerateClient ? clientGenerator.GenerateClient( feature, ns ) : null;
            var imports = ImportedNamespaces == null
                ? new string[0]
                : ImportedNamespaces.Split( new[] { ';' }, StringSplitOptions.RemoveEmptyEntries );
            channel.Debug( "Adding additional import statements" );
            AddUsingStatements( dtoUnit, imports );
            AddUsingStatements( providerUnit, imports );
            AddUsingStatements( clientUnit, imports );
            channel.Info( "Generating code for data transfer objects" );
            CodeGenerationHelper.GenerateCSharp( dtoUnit, Path.GetFullPath( DtoPath ) );
            channel.Info( "Generating code for provider" );
            if(GenerateServer)
            {
                CodeGenerationHelper.GenerateCSharp( providerUnit, Path.GetFullPath( ProviderPath ) );
            }
            if(GenerateClient)
            {
                CodeGenerationHelper.GenerateCSharp( clientUnit, Path.GetFullPath( GenerateServer ? ClientPath : ProviderPath ) );
            }
        }

        private static void AddUsingStatements( CodeCompileUnit unit, string[] imports )
        {
            if(imports == null || imports.Length == 0 || unit == null) return;
            var ns = unit.Namespaces.Cast<CodeNamespace>().FirstOrDefault( n => n.Name == null );
            if(ns == null)
            {
                ns = new CodeNamespace();
                unit.Namespaces.Add( ns );
            }

            foreach(var import in imports)
            {
                ns.Imports.Add( new CodeNamespaceImport( import ) );
            }
        }
    }
}
