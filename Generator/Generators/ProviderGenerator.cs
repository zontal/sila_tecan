using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using Grpc.Core;
using Tecan.MAP.Contracts.Services.Logging;
using Tecan.Sila2.Generator.Contracts;
using Tecan.Sila2.Generator.Helper;

namespace Tecan.Sila2.Generator.Generators
{
    /// <summary>
    /// Generates providers for features
    /// </summary>
    [Export(typeof(IProviderGenerator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ProviderGenerator : IProviderGenerator
    {
        private readonly ICodeNameProvider _nameProvider;
        private readonly IClientGenerator _clientGenerator;
        private readonly IServerGenerator _serverGenerator;
        private readonly ITypeTranslationProvider _translationProvider;
        private readonly IEnumerable<IGeneratorHook> _hooks;
        private readonly ILoggingChannel _loggingChannel;


        /// <summary>
        /// Creates a new provider generator
        /// </summary>
        /// <param name="nameProvider">The name provider used to generate feature names</param>
        /// <param name="clientGenerator">The generator for the client class</param>
        /// <param name="serverGenerator">The generator for the server class</param>
        /// <param name="translationProvider">A component that translates types between SiLA2 and .NET</param>
        /// <param name="loggingChannelFactory">The logging component</param>
        /// <param name="hooks">Code generator hooks</param>
        [ImportingConstructor]
        public ProviderGenerator(ICodeNameProvider nameProvider, IClientGenerator clientGenerator,
            IServerGenerator serverGenerator, ITypeTranslationProvider translationProvider, ILoggingChannelFactory loggingChannelFactory, [ImportMany] IEnumerable<IGeneratorHook> hooks)
        {
            _nameProvider = nameProvider;
            _clientGenerator = clientGenerator;
            _serverGenerator = serverGenerator;
            _hooks = hooks?.ToList();
            _translationProvider = translationProvider;
            _loggingChannel = loggingChannelFactory.CreateChannel( typeof(ProviderGenerator) );
        }

        /// <inheritdoc />
        public CodeCompileUnit GenerateInterfaceUnit(Feature feature, string ns, bool generateServer,
            bool generateClient)
        {
            _loggingChannel.Log($"Generating methods container for {feature.FullyQualifiedIdentifier}".ToString, Severity.Info);
            var unit = new CodeCompileUnit();
            var nSpace = new CodeNamespace(ns);
            unit.Namespaces.Add(nSpace);
            nSpace.Types.Add(GenerateProviderClass(feature, generateServer, generateClient, nSpace));

            if (generateServer && feature.Items != null)
            {
                foreach (var metadata in feature.Items.OfType<FeatureMetadata>())
                {
                    nSpace.Types.Add(_serverGenerator.GenerateMetadataInterceptorBase(feature, metadata));
                }
            }

            var usingNs = new CodeNamespace();
            usingNs.Imports.Add(new CodeNamespaceImport("System.Linq"));
            usingNs.Imports.Add(new CodeNamespaceImport("Tecan.Sila2"));
            usingNs.Imports.Add(new CodeNamespaceImport("Tecan.Sila2.Client"));
            usingNs.Imports.Add(new CodeNamespaceImport("Tecan.Sila2.Server"));
            unit.Namespaces.Add(usingNs);

            if( _hooks != null )
            {
                foreach( var generatorHook in _hooks )
                {
                    _loggingChannel.Log($"Executing hook {generatorHook} after code generation completed".ToString);
                    generatorHook?.OnCodeGenerationComplete(feature, unit);
                }
            }

            return unit;
        }

        private CodeTypeDeclaration GenerateProviderClass(Feature feature, bool generateServer, bool generateClient, CodeNamespace ns)
        {
            var provider = new CodeTypeDeclaration(feature.Identifier + "Methods")
            {
                Attributes = MemberAttributes.Assembly | MemberAttributes.Static,
                IsClass = true,
                IsPartial = true,
                TypeAttributes = TypeAttributes.Sealed | TypeAttributes.NestedAssembly
            };

            if( feature.Items != null )
            {
                var serviceNameRef = GenerateServiceNameField( feature, provider );

                foreach( var featureCommand in feature.Items.OfType<FeatureCommand>() )
                {
                    GenerateCommandMethod( featureCommand, provider, serviceNameRef );
                }

                foreach( var featureProperty in feature.Items.OfType<FeatureProperty>() )
                {
                    GeneratePropertyMethod( featureProperty, provider, serviceNameRef );
                }

                foreach( var metadata in feature.Items.OfType<FeatureMetadata>() )
                {
                    GenerateMetadataMethod( metadata, provider, serviceNameRef );
                }
            }


            if (generateServer && _serverGenerator != null)
            {
                ns.Types.Add(_serverGenerator.GenerateServer(feature));
            }
            if (generateClient && _clientGenerator != null)
            {
                ns.Types.Add(_clientGenerator.GenerateClient(feature));
            }

            provider.WriteDocumentation($"Encapsulates provider classes to expose the {feature.DisplayName} feature via SiLA2");

            return provider;
        }

        private void GenerateMetadataMethod(FeatureMetadata metadata, CodeTypeDeclaration provider, CodeFieldReferenceExpression serviceNameRef)
        {
            provider.Members.Add(GenerateMethod(
                MethodType.Unary,
                "Get_FCPAffectedByMetadata_" + metadata.Identifier,
                "GetAffectedBy" + metadata.Identifier + "Method",
                null, new CodeTypeReference(typeof(List<StringDto>)), serviceNameRef));
        }

        private void GeneratePropertyMethod(FeatureProperty featureProperty, CodeTypeDeclaration provider,
            CodeFieldReferenceExpression serviceName)
        {
            var propertyResponse = _nameProvider.GetPropertyResponseType( featureProperty, _translationProvider);
            if (featureProperty.Observable == FeaturePropertyObservable.No)
            {
                provider.Members.Add(GenerateMethod(
                    MethodType.Unary,
                    "Get_" + featureProperty.Identifier, "Get" + featureProperty.Identifier + "Method",
                    null,
                    propertyResponse, serviceName));
            }
            else
            {
                provider.Members.Add(GenerateMethod(
                    MethodType.ServerStreaming,
                    "Subscribe_" + featureProperty.Identifier, "Subscribe" + featureProperty.Identifier + "Method",
                    null,
                    propertyResponse, serviceName));
            }
        }

        private void GenerateCommandMethod(FeatureCommand featureCommand, CodeTypeDeclaration provider,
            CodeFieldReferenceExpression serviceName)
        {
            var responseType = _nameProvider.GenerateCommandResponseType(featureCommand);
            if (featureCommand.Observable == FeatureCommandObservable.No)
            {
                provider.Members.Add(GenerateMethod(
                    MethodType.Unary,
                    featureCommand.Identifier, featureCommand.Identifier + "Method",
                    _nameProvider.GenerateCommandRequestType(featureCommand),
                    _nameProvider.GenerateCommandResponseType(featureCommand), serviceName));
            }
            else
            {
                provider.Members.Add(GenerateMethod(
                    MethodType.Unary,
                    featureCommand.Identifier, featureCommand.Identifier + "Method",
                    _nameProvider.GenerateCommandRequestType(featureCommand),
                    new CodeTypeReference(nameof(CommandConfirmation)), serviceName));
                provider.Members.Add(GenerateMethod(
                    MethodType.ServerStreaming,
                    featureCommand.Identifier + "_Info", featureCommand.Identifier + "StateMethod",
                    new CodeTypeReference(nameof(CommandExecutionUuid)),
                    new CodeTypeReference(nameof(ExecutionState)), serviceName));
                if (featureCommand.IntermediateResponse != null && featureCommand.IntermediateResponse.Length > 0)
                {
                    provider.Members.Add(GenerateMethod(
                        MethodType.Unary,
                        featureCommand.Identifier + "_IntermediateResponses", featureCommand.Identifier + "IntermediateMethod",
                        new CodeTypeReference(nameof(CommandExecutionUuid)),
                        _nameProvider.GenerateCommandIntermediateType(featureCommand), serviceName));

                }
                provider.Members.Add(GenerateMethod(
                    MethodType.Unary,
                    featureCommand.Identifier + "_Result", featureCommand.Identifier + "ResponseMethod",
                    new CodeTypeReference(nameof(CommandExecutionUuid)),
                    _nameProvider.GenerateCommandResponseType(featureCommand), serviceName));
            }
        }

        private CodeMemberField GenerateMethod(MethodType methodType, string name, string fieldName,
            CodeTypeReference requestType,
            CodeTypeReference responseType, CodeFieldReferenceExpression serviceName)
        {
            var fieldType = new CodeTypeReference(typeof(Method<,>).FullName,
                requestType ?? new CodeTypeReference(nameof(EmptyRequest)), responseType);
            var methodTypeExpression = new CodeFieldReferenceExpression(
                new CodeTypeReferenceExpression(typeof(MethodType)), methodType.ToString());
            var requestMarshaller = GenerateMarshaller(requestType);
            var responseMarshaller = GenerateMarshaller(responseType);
            return new CodeMemberField
            {
                Name = fieldName,
                Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static,
                Type = fieldType,
                InitExpression = new CodeObjectCreateExpression(
                    fieldType,
                    methodTypeExpression,
                    serviceName,
                    new CodePrimitiveExpression(name),
                    requestMarshaller,
                    responseMarshaller)
            };
        }

        private static CodeExpression GenerateMarshaller(CodeTypeReference requestType)
        {
            CodeExpression requestMarshaller;
            if (requestType != null && requestType.BaseType != nameof(EmptyRequest))
            {
                requestMarshaller = new CodePropertyReferenceExpression(new CodeTypeReferenceExpression
                {
                    Type = new CodeTypeReference(typeof(ProtobufMarshaller<>).Name, requestType)
                }, "Default");
            }
            else
            {
                requestMarshaller =
                    new CodePropertyReferenceExpression(new CodeTypeReferenceExpression(nameof(EmptyRequest)),
                        "Marshaller");
            }

            return requestMarshaller;
        }

        private CodeFieldReferenceExpression GenerateServiceNameField(Feature feature, CodeTypeDeclaration provider)
        {
            var serviceNameField = new CodeMemberField
            {
                Name = "__ServiceName",
                Type = new CodeTypeReference(typeof(string)),
                Attributes = MemberAttributes.Private | MemberAttributes.Static,
                InitExpression = new CodePrimitiveExpression(feature.Namespace + "." + feature.Identifier)
            };
            provider.Members.Add(serviceNameField);
            return new CodeFieldReferenceExpression(null, serviceNameField.Name);
        }
    }
}
