using Common.Logging;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tecan.Sila2.Cancellation;
using Tecan.Sila2.Client;
using Tecan.Sila2.Generator.Contracts;
using Tecan.Sila2.Generator.Helper;

#pragma warning disable S3265 // Non-flags enums should not be used in bitwise operations

namespace Tecan.Sila2.Generator.Generators
{
    /// <summary>
    /// Generates client classes that implement a feature interface using SiLA
    /// </summary>
    [Export( typeof( IClientGenerator ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class ClientGenerator : IClientGenerator
    {
        private const string ExecutionManagerFieldName = "_executionManager";
        private const string ChannelFieldName = "_channel";
        private const string ServiceNameFieldName = "_serviceName";
        private const string CancellationTokenSourceFieldName = "_cancellationTokenSource";

        private readonly ICodeNameProvider _nameProvider;
        private readonly IEnumerable<IGeneratorHook> _hooks;
        private readonly ITypeTranslationProvider _translationProvider;
        private readonly ILog _loggingChannel = LogManager.GetLogger<ClientGenerator>();
        private readonly IDependencyInjectionGenerator _diGenerator;
        private readonly IGeneratorConfigSource _generatorConfigSource;

        /// <summary>
        /// Creates a new client generator
        /// </summary>
        /// <param name="nameProvider">The name provider used to generate feature names</param>
        /// <param name="translationProvider">A component used to translate types between SiLA2 and .NET</param>
        /// <param name="hooks">Generator hooks</param>
        /// <param name="diGenerator">A generator for the dependency injection registration</param>
        /// <param name="configSource">A component from which generator specifications can be drawn</param>
        [ImportingConstructor]
        public ClientGenerator( ICodeNameProvider nameProvider, ITypeTranslationProvider translationProvider, [ImportMany] IEnumerable<IGeneratorHook> hooks, IDependencyInjectionGenerator diGenerator, IGeneratorConfigSource configSource )
        {
            _nameProvider = nameProvider;
            _hooks = hooks;
            _translationProvider = translationProvider;
            _diGenerator = diGenerator;
            _generatorConfigSource = configSource;
        }

        /// <inheritdoc />
        public CodeCompileUnit GenerateClient( Feature feature, string ns )
        {
            _loggingChannel.Info( $"Generating clients for {feature.FullyQualifiedIdentifier}" );
            var unit = new CodeCompileUnit();
            var nSpace = new CodeNamespace( ns );
            unit.Namespaces.Add( nSpace );

            var client = GenerateClientClass( feature );
            nSpace.Types.Add( client );
            nSpace.Types.Add( GenerateClientFactory( feature ) );

            var usingNs = new CodeNamespace();
            usingNs.Imports.Add( new CodeNamespaceImport( "System.Linq" ) );
            usingNs.Imports.Add( new CodeNamespaceImport( "Tecan.Sila2" ) );
            usingNs.Imports.Add( new CodeNamespaceImport( "Tecan.Sila2.Client" ) );
            usingNs.Imports.Add( new CodeNamespaceImport( "Tecan.Sila2.Server" ) );
            unit.Namespaces.Add( usingNs );

            if(_hooks != null)
            {
                foreach(var generatorHook in _hooks)
                {
                    _loggingChannel.Debug( $"Executing hook {generatorHook} after code generation completed" );
                    generatorHook?.OnClientGenerated( feature, client, unit );
                }
            }

            return unit;
        }

        private CodeTypeDeclaration GenerateClientFactory( Feature feature )
        {
            _loggingChannel.Info( $"Generating client factory for feature {feature.FullyQualifiedIdentifier}" );
            var client = new CodeTypeDeclaration( feature.Identifier + "ClientFactory" )
            {
                Attributes = MemberAttributes.Public,
                TypeAttributes = TypeAttributes.Public,
                IsClass = true,
                IsPartial = true
            };
            client.BaseTypes.Add( typeof( IClientFactory ).Name );

            client.Members.Add( (new CodeMemberProperty
            {
                Name = nameof( IClientFactory.FeatureIdentifier ),
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                Type = new CodeTypeReference( typeof( string ) ),
                HasGet = true,
                HasSet = false,
                GetStatements =
                {
                    new CodeMethodReturnStatement(new CodePrimitiveExpression(feature.FullyQualifiedIdentifier))
                }
            }).WriteDocumentation( "Gets the fully-qualified identifier of the feature for which clients can be generated" ) );

            client.Members.Add( (new CodeMemberProperty
            {
                Name = nameof( IClientFactory.InterfaceType ),
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                Type = new CodeTypeReference( typeof( Type ) ),
                HasGet = true,
                HasSet = false,
                GetStatements =
                {
                    new CodeMethodReturnStatement(new CodeTypeOfExpression(_nameProvider.CreateFeatureInterfaceReference(feature)))
                }
            }).WriteDocumentation( "Gets the interface type for which clients can be generated" ) );

            var channelParameter = new CodeParameterDeclarationExpression( typeof( IClientChannel ).Name, "channel" );
            var executionManagerParameter = new CodeParameterDeclarationExpression( typeof( IClientExecutionManager ).Name, "executionManager" );

            client.Members.Add( (new CodeMemberMethod
            {
                Name = nameof( IClientFactory.CreateClient ),
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                ReturnType = new CodeTypeReference( typeof( object ) ),
                Parameters = { channelParameter, executionManagerParameter },
                Statements =
                {
                    new CodeMethodReturnStatement(new CodeObjectCreateExpression(feature.Identifier + "Client",
                        new CodeArgumentReferenceExpression(channelParameter.Name),
                        new CodeArgumentReferenceExpression(executionManagerParameter.Name)))
                }
            }).WriteDocumentation( summary: "Creates a strongly typed client for the given execution channel and execution manager",
                                   returns: "A strongly typed client. This object will be an instance of the InterfaceType property",
                                   parameters: new Dictionary<string, string>
                                   {
                                       {channelParameter.Name, "The channel that should be used for communication with the server" },
                                       {executionManagerParameter.Name, "The execution manager to manage metadata" }
                                   } ) );

            client.WriteDocumentation( $"Factory to instantiate clients for the {feature.DisplayName}." );

            _diGenerator.AddDependencyInjectionRegistrations( client );
            return client;
        }


        /// <inheritdoc />
        public CodeTypeDeclaration GenerateClientClass( Feature feature )
        {
            _loggingChannel.Info( $"Generating client for feature {feature.FullyQualifiedIdentifier}" );
            var client = new CodeTypeDeclaration( feature.Identifier + "Client" )
            {
                Attributes = MemberAttributes.Public,
                TypeAttributes = TypeAttributes.Public,
                IsClass = true,
                IsPartial = true
            };
            client.BaseTypes.Add( _nameProvider.CreateFeatureInterfaceReference( feature ) );
            var spec = _generatorConfigSource.GetFeatureSpec( feature.Identifier );

            GenerateClientBaseImplementation( feature, spec, client );

            if(feature.Items != null)
            {
                foreach(var featureCommand in feature.Items.OfType<FeatureCommand>())
                {
                    var commandSpec = spec?.Command?.FirstOrDefault( c => featureCommand.Identifier == (c.Identifier ?? c.Code) );
                    if(!CodeGenerationHelper.IsSetterCommand( featureCommand, feature.Items.OfType<FeatureProperty>(), out var property ))
                    {
                        _loggingChannel.Debug( $"Generate client command for {featureCommand.Identifier}" );
                        GenerateClientCommand( feature, featureCommand, commandSpec, client );
                    }
                    else
                    {
                        _loggingChannel.Debug( $"Skipping {featureCommand.Identifier} because it is a property setter of property {property.Identifier}" );
                    }
                }
            }
            else
            {
                _loggingChannel.Warn( $"The feature {feature.Identifier} has no items." );
            }
            GenerateClientServiceNameField( feature, client );
            GenerateClientExtractMethod( client );

            client.WriteDocumentation( $"Class that implements the {_nameProvider.CreateFeatureInterfaceReference( feature ).BaseType} interface through SiLA2" );
            return client;
        }

        private void GenerateClientBaseImplementation( Feature feature, FeatureSpec spec, CodeTypeDeclaration client )
        {
            var initLazyRequests = new CodeMemberMethod()
            {
                Name = "InitLazyRequests",
                Attributes = MemberAttributes.Private | MemberAttributes.Final
            };
            initLazyRequests.WriteDocumentation( "Initializes lazies for non-observable properties." );
            var anyLazy = false;
            var anyDynamic = false;
            if(feature.Items != null)
            {
                foreach(var featureProperty in feature.Items.OfType<FeatureProperty>())
                {
                    _loggingChannel.Debug( $"Generate property {featureProperty.Identifier}" );
                    var propertySpec = spec?.Property?.FirstOrDefault( p => featureProperty.Identifier == (p.Identifier ?? p.Code) );
                    var setterCommand = feature.Items.OfType<FeatureCommand>().FirstOrDefault( c => CodeGenerationHelper.IsSetterCommand( c, featureProperty ) );
                    anyLazy |= featureProperty.Observable == FeaturePropertyObservable.No && setterCommand == null && (propertySpec == null || !propertySpec.LazySpecified || propertySpec.Lazy);
                    anyDynamic |= featureProperty.Observable == FeaturePropertyObservable.Yes && setterCommand == null;
                    GenerateClientProperty( feature, featureProperty, propertySpec, client, initLazyRequests, setterCommand );
                }
            }

            if(anyDynamic)
            {
                _loggingChannel.Debug( "Client contains dynamic properties. Implementing INotifyPropertyChanged and IDisposable." );
                client.BaseTypes.Add( typeof( INotifyPropertyChanged ) );
                client.BaseTypes.Add( typeof( IDisposable ) );
                client.Members.Add( new CodeMemberEvent()
                {
                    Name = nameof( INotifyPropertyChanged.PropertyChanged ),
                    Attributes = MemberAttributes.Public,
                    Type = new CodeTypeReference( typeof( PropertyChangedEventHandler ) )
                } );
                client.Members.Add( new CodeMemberField
                {
                    Name = CancellationTokenSourceFieldName,
                    Attributes = MemberAttributes.Private,
                    Type = new CodeTypeReference( typeof( CancellationTokenSource ) ),
                    InitExpression = new CodeObjectCreateExpression( typeof( CancellationTokenSource ) )
                } );
                var ctsRef = new CodeFieldReferenceExpression( null, CancellationTokenSourceFieldName );
                client.Members.Add( (new CodeMemberMethod
                {
                    Name = nameof( IDisposable.Dispose ),
                    Attributes = MemberAttributes.Public | MemberAttributes.Final,
                    Statements =
                    {
                        new CodeMethodInvokeExpression(ctsRef, nameof(CancellationTokenSource.Cancel)),
                        new CodeMethodInvokeExpression(ctsRef, nameof(CancellationTokenSource.Dispose))
                    }
                }).WriteDocumentation( "Disposes the current client and cancels all subscriptions" ) );
            }

            if(anyLazy) client.Members.Add( initLazyRequests );
            GenerateClientConstructors( client, anyLazy );
        }

        private void GenerateClientServiceNameField( Feature feature, CodeTypeDeclaration client )
        {
            client.Members.Add( new CodeMemberField
            {
                Name = ServiceNameFieldName,
                Type = new CodeTypeReference( typeof( string ) ),
                Attributes = MemberAttributes.Const | MemberAttributes.Private,
                InitExpression = new CodePrimitiveExpression( feature.Namespace + "." + feature.Identifier )
            } );
        }

        private void GenerateClientExtractMethod( CodeTypeDeclaration client )
        {
            var T = new CodeTypeReference( "T" );
            var extract = new CodeMemberMethod()
            {
                Name = nameof( ISilaTransferObject<object>.Extract ),
                Attributes = MemberAttributes.Private,
                ReturnType = T
            };
            extract.TypeParameters.Add( new CodeTypeParameter( "T" ) );
            extract.Parameters.Add(
                new CodeParameterDeclarationExpression( new CodeTypeReference( typeof( ISilaTransferObject<> ).FullName, T ),
                    "dto" ) );
            extract.Statements.Add( new CodeMethodReturnStatement(
                new CodeMethodInvokeExpression( new CodeArgumentReferenceExpression( "dto" ), extract.Name, new CodePropertyReferenceExpression(
                    new CodeFieldReferenceExpression( null, ExecutionManagerFieldName ),
                    nameof( IClientExecutionManager.DownloadBinaryStore ) ) ) ) );
            client.Members.Add( extract );
        }

        private void GenerateClientCommand( Feature feature, FeatureCommand featureCommand, CommandSpec spec, CodeTypeDeclaration client )
        {
            var requestMethod = new CodeMemberMethod()
            {
                Name = spec?.Code ?? _nameProvider.GetCommandName(featureCommand),
                Attributes = MemberAttributes.Public
            };
            var parameterDictionary = new Dictionary<string, string>();
            client.Members.Add(requestMethod);
            var requestType = _nameProvider.GenerateCommandRequestType(featureCommand);
            var request = new CodeObjectCreateExpression(requestType);
            var requestRef = new CodeVariableReferenceExpression(nameof(request));
            string binaryParameterIdentifier = null;
            var method = _nameProvider.GetNonstandardMethod(featureCommand);
            if (method == null)
            {
                GenerateClientCommandSignature(featureCommand, spec, requestMethod, parameterDictionary, request);

                var binaryParameter = featureCommand.Parameter?.SingleOrDefault(p =>
                    ContainsBinaries(p.DataType, id => feature.Items.OfType<SiLAElement>().Single(e => e.Identifier == id).DataType));
                if (binaryParameter != null)
                {
                    binaryParameterIdentifier = feature.GetFullyQualifiedParameterIdentifier(featureCommand, binaryParameter.Identifier);
                }
            }
            else
            {
                var parameters = method.GetParameters();
                foreach (var parameterInfo in parameters)
                {
                    requestMethod.Parameters.Add(new CodeParameterDeclarationExpression(parameterInfo.ParameterType, parameterInfo.Name));
                    if (parameterInfo.ParameterType != typeof(CancellationToken))
                    {
                        request.Parameters.Add(new CodeArgumentReferenceExpression(parameterInfo.Name));
                    }
                }
                requestMethod.WriteInheritDoc();
            }

            request.Parameters.Add(GenerateCreateBinaryStore(binaryParameterIdentifier));
            requestMethod.Statements.Add(new CodeVariableDeclarationStatement(requestType, nameof(request), request));
            requestMethod.Statements.Add(new CodeMethodInvokeExpression(requestRef, nameof(Argument.Validate)));

            GenerateCommandImplementation(feature, featureCommand, client, requestMethod, requestRef, method);
        }

        private void GenerateCommandImplementation(Feature feature, FeatureCommand featureCommand, CodeTypeDeclaration client, CodeMemberMethod requestMethod, CodeVariableReferenceExpression requestRef, MethodInfo method)
        {
            if (featureCommand.Observable == FeatureCommandObservable.No)
            {
                requestMethod.ReturnType = GetClientRequestMethodResponseType(featureCommand);
                GenerateClientNonObservableCommandImplementation(feature, featureCommand, client, requestMethod.Statements, requestRef, requestMethod.ReturnType);
            }
            else
            {
                var commandCall = GenerateClientObservableCommandImplementation(feature, featureCommand, client, requestRef);
                if (method == null)
                {
                    requestMethod.ReturnType = _nameProvider?.GetObservableCommandReturnType(featureCommand, _translationProvider);
                    requestMethod.Statements.Add(new CodeMethodReturnStatement(commandCall));
                }
                else
                {
                    requestMethod.ReturnType = new CodeTypeReference(method.ReturnType);

                    if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(IObservable<>))
                    {
                        var createCommand = new CodeObjectCreateExpression(
                            new CodeTypeReference(typeof(CommandObservable<>).Name, new CodeTypeReference(method.ReturnType.GetGenericArguments()[0])),
                            commandCall);
                        requestMethod.Statements.Add(new CodeMethodReturnStatement(createCommand));
                    }
                    else
                    {
                        GenerateNonstandardObservableCommandBody(featureCommand, requestMethod, commandCall, method);
                    }
                }
            }
        }

        private void GenerateClientCommandSignature(FeatureCommand featureCommand, CommandSpec spec, CodeMemberMethod requestMethod, Dictionary<string, string> parameterDictionary, CodeObjectCreateExpression request)
        {
            if (featureCommand.Parameter != null)
            {
                foreach (var parameterType in featureCommand.Parameter)
                {
                    var parameterSpec = spec?.Parameter?.FirstOrDefault(p => string.Equals(p.Identifier ?? p.Key, parameterType.Identifier, StringComparison.OrdinalIgnoreCase));
                    var parameterName = parameterSpec?.Key ?? parameterType.Identifier.ToCamelCase();
                    var origin = featureCommand.Identifier + "." + parameterType.Identifier;
                    if (parameterSpec?.Mapping?.ValueExpression is PropertyExpression propertyExpression)
                    {
                        request.Parameters.Add(new CodePropertyReferenceExpression(new CodeArgumentReferenceExpression(parameterName), propertyExpression.Property));
                    }
                    else
                    {
                        request.Parameters.Add(new CodeArgumentReferenceExpression(parameterName));
                    }
                    requestMethod.Parameters.Add(new CodeParameterDeclarationExpression(
                        _nameProvider?.GetMemberType(origin)
                        ?? _translationProvider.ExtractType(parameterType.DataType, featureCommand.Identifier + parameterType.Identifier), parameterName));

                    parameterDictionary.Add(parameterName, parameterSpec?.Description ?? parameterType.Description);
                }

            }

            requestMethod.WriteDocumentation(spec?.Description ?? featureCommand.Description, parameters: parameterDictionary);
        }

        private CodeExpression FollowExpression( CodeExpression expression, Expression toFollow )
        {
            switch(toFollow)
            {
                case FormatExpression format:
                    var arguments = new List<CodeExpression>();
                    arguments.Add( new CodePrimitiveExpression( format.FormatString ) );
                    arguments.AddRange( format.Arg.Select( e => FollowExpression( expression, e ) ) );
                    return new CodeMethodInvokeExpression(
                        new CodeMethodReferenceExpression( new CodeTypeReferenceExpression( typeof( string ) ), nameof( string.Format ) ),
                        arguments.ToArray() );
                case PropertyExpression propertyExpression:
                    return new CodePropertyReferenceExpression( expression, propertyExpression.Property );
                default:
                    return expression;
            }
        }

        private void GenerateNonstandardObservableCommandBody( FeatureCommand featureCommand, CodeMemberMethod requestMethod,
            CodeMethodInvokeExpression commandCall, MethodInfo method )
        {
            var command = new CodeVariableReferenceExpression( "command" );
            CodeArgumentReferenceExpression cancellationToken = null;
            requestMethod.Statements.Add( new CodeVariableDeclarationStatement( _nameProvider?.GetObservableCommandReturnType( featureCommand, _translationProvider ),
                command.VariableName, commandCall ) );
            if(method.GetParameters() is var parameters && parameters.Length > 0 &&
                parameters[parameters.Length - 1].ParameterType == typeof( CancellationToken ))
            {
                cancellationToken = new CodeArgumentReferenceExpression( parameters[parameters.Length - 1].Name );
                requestMethod.Statements.Add( new CodeMethodInvokeExpression( new CodeTypeReferenceExpression( typeof( CancellationHelper ) ), nameof( CancellationHelper.RegisterCancellation ),
                    new CodeCastExpression( typeof( IClientCommand ), command ),
                    new CodeFieldReferenceExpression( null, ExecutionManagerFieldName ),
                    new CodeFieldReferenceExpression( null, ChannelFieldName ),
                    cancellationToken ) );
            }

            requestMethod.Statements.Add( new CodeMethodInvokeExpression( command, nameof( IObservableCommand.Start ) ) );
            var response = new CodePropertyReferenceExpression( command, nameof( IObservableCommand.Response ) );
            if(typeof( Task ).IsAssignableFrom( method.ReturnType ))
            {
                requestMethod.Statements.Add( new CodeMethodReturnStatement( response ) );
            }
            else
            {
                var wait = new CodeMethodInvokeExpression( response, nameof( Task.Wait ) );
                if(cancellationToken != null)
                {
                    wait.Parameters.Add( cancellationToken );
                }

                requestMethod.Statements.Add( wait );
                if(method.ReturnType.IsGenericType || !typeof( Task ).IsAssignableFrom( method.ReturnType ))
                {
                    requestMethod.Statements.Add(
                        new CodeMethodReturnStatement(
                            new CodePropertyReferenceExpression( response, nameof( Task<object>.Result ) ) ) );
                }
            }
        }

        private bool ContainsBinaries( DataTypeType dataType, Func<string, DataTypeType> typeResolver )
        {
            switch(dataType.Item)
            {
                case BasicType basic:
                    return basic == BasicType.Binary;
                case StructureType structured:
                    return structured.Element == null || structured.Element.Any( e => ContainsBinaries( e.DataType, typeResolver ) );
                case ListType list:
                    return ContainsBinaries( list.DataType, typeResolver );
                case string identifiedType:
                    return ContainsBinaries( typeResolver( identifiedType ), typeResolver );
                default:
                    return false;
            }
        }

        private static CodeExpression GenerateCreateBinaryStore( string commandParameterIdentifier )
        {
            if(commandParameterIdentifier == null)
            {
                return new CodePrimitiveExpression();
            }
            return new CodeMethodInvokeExpression( new CodeFieldReferenceExpression( null, ExecutionManagerFieldName ), nameof( IClientExecutionManager.CreateBinaryStore ), new CodePrimitiveExpression( commandParameterIdentifier ) );
        }

        private CodeMethodInvokeExpression GenerateClientObservableCommandImplementation( Feature feature, FeatureCommand featureCommand,
            CodeTypeDeclaration client,
            CodeVariableReferenceExpression requestRef )
        {
            var exceptionConverter =
                GenerateClientConvertExceptionMethod( feature, featureCommand.Identifier, featureCommand.DefinedExecutionErrors );
            client.Members.Add( exceptionConverter );
            CodeTypeReference intermediateType = _nameProvider?.GetIntermediateType( featureCommand, _translationProvider );
            var callInfo = new CodeMethodInvokeExpression( new CodeFieldReferenceExpression( null, ExecutionManagerFieldName ),
                nameof( IClientExecutionManager.CreateCallOptions ),
                new CodePropertyReferenceExpression( requestRef, nameof( ISilaRequestObject.CommandIdentifier ) ) );

            var executeMethod = new CodeMethodReferenceExpression(
                new CodeFieldReferenceExpression( null, ChannelFieldName ),
                intermediateType != null ? nameof( IClientChannel.ExecuteIntermediatesCommand ) : nameof( IClientChannel.ExecuteObservableCommand ),
                _nameProvider?.GenerateCommandRequestType( featureCommand ) );

            var actualCall = new CodeMethodInvokeExpression(
                executeMethod,
                new CodeFieldReferenceExpression( null, ServiceNameFieldName ),
                new CodePrimitiveExpression( featureCommand.Identifier ),
                requestRef );

            if(intermediateType != null)
            {
                executeMethod.TypeArguments.Add( intermediateType );
                executeMethod.TypeArguments.Add( _nameProvider?.GenerateCommandIntermediateType( featureCommand ) );

                var convertIntermediateResponseMethod = GenerateClientCommandResponseMethod(
                    featureCommand.Identifier + "Intermediate",
                    featureCommand.IntermediateResponse,
                    intermediateType,
                    client );
                actualCall.Parameters.Add( new CodeMethodReferenceExpression( null, convertIntermediateResponseMethod.Name ) );
            }
            if(featureCommand.Response != null && featureCommand.Response.Length > 0)
            {
                var responseType = _nameProvider?.GetMemberType( featureCommand.Identifier + "." + featureCommand.Response[0].Identifier )
                    ?? _translationProvider.ExtractType( featureCommand.Response[0].DataType, featureCommand.Identifier );

                var convertResponseMethod = GenerateClientCommandResponseMethod(
                    featureCommand.Identifier + "Response",
                    featureCommand.Response, responseType,
                    client );
                executeMethod.TypeArguments.Add( responseType );
                executeMethod.TypeArguments.Add( _nameProvider?.GenerateCommandResponseType( featureCommand ) );
                actualCall.Parameters.Add( new CodeMethodReferenceExpression( null, convertResponseMethod.Name ) );
            }
            actualCall.Parameters.Add( new CodeMethodReferenceExpression( null, exceptionConverter.Name ) );
            actualCall.Parameters.Add( callInfo );

            return actualCall;
        }

        private CodeMemberMethod GenerateClientCommandResponseMethod( string name, SiLAElement[] elements, CodeTypeReference returnType,
            CodeTypeDeclaration client )
        {
            var convertResponseMethod = new CodeMemberMethod
            {
                Name = "Convert" + name,
                Attributes = MemberAttributes.Private,
                ReturnType = returnType
            };
            var valueParameter =
                new CodeParameterDeclarationExpression( name + "Dto", "value" );
            convertResponseMethod.Parameters.Add( valueParameter );
            convertResponseMethod.WriteDocumentation( $"Unwraps the response of the {name} command", "The actual response", new Dictionary<string, string>()
            {
                {valueParameter.Name, "The response data transfer object" }
            } );
            convertResponseMethod.Statements.Add( new CodeMethodReturnStatement( GenerateClientConvertResponse(
                elements[0].DataType,
                elements[0].Identifier,
                new CodeArgumentReferenceExpression( valueParameter.Name ),
                _nameProvider.GetMemberType( name + ".Return" ) ) ) );
            client.Members.Add( convertResponseMethod );
            return convertResponseMethod;
        }

        private CodeTypeReference GetClientRequestMethodResponseType( FeatureCommand featureCommand )
        {
            if(featureCommand.Response != null && featureCommand.Response.Length > 0)
            {
                if(featureCommand.Response.Length == 1)
                {
                    var customType = _nameProvider.GetMemberType( featureCommand.Identifier + "." + featureCommand.Response[0].Identifier );
                    return customType ?? _translationProvider.ExtractType( featureCommand.Response[0].DataType, featureCommand.Identifier + featureCommand.Response[0].Identifier );
                }
                else
                {
                    return new CodeTypeReference( featureCommand.Identifier + "Response" );
                }
            }

            return new CodeTypeReference( typeof( void ) );
        }

        private void GenerateClientNonObservableCommandImplementation( Feature feature, FeatureCommand featureCommand,
            CodeTypeDeclaration client,
            CodeStatementCollection requestMethod, CodeVariableReferenceExpression requestRef, CodeTypeReference returnType )
        {
            var identifier = feature.GetFullyQualifiedIdentifier( featureCommand );
            var callInfo = new CodeMethodInvokeExpression(
                new CodeFieldReferenceExpression( null, ExecutionManagerFieldName ),
                nameof( IClientExecutionManager.CreateCallOptions ),
                new CodePrimitiveExpression( identifier ) );
            var callInfoRef = new CodeVariableReferenceExpression( nameof( callInfo ) );
            requestMethod.Add( new CodeVariableDeclarationStatement( typeof( IClientCallInfo ), callInfoRef.VariableName, callInfo ) );

            var executeMethod = new CodeMethodReferenceExpression(
                new CodeFieldReferenceExpression( null, ChannelFieldName ),
                nameof( IClientChannel.ExecuteUnobservableCommand ),
                _nameProvider.GenerateCommandRequestType( featureCommand ) );
            var serviceName = new CodeFieldReferenceExpression( null, ServiceNameFieldName );

            var executeCall = new CodeMethodInvokeExpression( executeMethod,
                serviceName,
                new CodePrimitiveExpression( featureCommand.Identifier ),
                requestRef,
                callInfoRef );
            CodeExpression convertedCall;
            if(featureCommand.Response != null && featureCommand.Response.Length > 0)
            {
                executeMethod.TypeArguments.Add( _nameProvider.GenerateCommandResponseType( featureCommand ) );
                if(featureCommand.Response.Length == 1)
                {
                    var customType = _nameProvider.GetMemberType( featureCommand.Identifier + "." + featureCommand.Response[0].Identifier );
                    convertedCall = GenerateClientConvertResponse( featureCommand.Response[0].DataType,
                        featureCommand.Response[0].Identifier,
                        executeCall, customType );
                }
                else
                {
                    convertedCall = new CodeMethodInvokeExpression( executeCall,
                        nameof( ISilaTransferObject<object>.Extract ),
                        new CodePropertyReferenceExpression(
                            new CodeFieldReferenceExpression( null, ExecutionManagerFieldName ),
                            nameof( IClientExecutionManager.DownloadBinaryStore ) ) );
                }
            }
            else
            {
                convertedCall = executeCall;
            }

            requestMethod.Add( GenerateClientExceptionHandling( feature, client, convertedCall, featureCommand.Response != null && featureCommand.Response.Length > 0 ? returnType : null,
                    featureCommand.DefinedExecutionErrors, featureCommand.Identifier, callInfoRef ) );
        }

        private CodeTryCatchFinallyStatement GenerateClientExceptionHandling( Feature feature, CodeTypeDeclaration client, CodeExpression response, CodeTypeReference responseType, string[] errors,
            string name, CodeExpression callInfo )
        {
            var tryStmt = new CodeTryCatchFinallyStatement();
            var catchAll = new CodeCatchClause( "ex", new CodeTypeReference( typeof( Exception ) ) );
            tryStmt.CatchClauses.Add( catchAll );
            var errorHandler =
                new CodeMethodInvokeExpression( new CodeFieldReferenceExpression( null, ChannelFieldName ),
                    nameof( IClientChannel.ConvertException ) );
            var exceptionRef = new CodeVariableReferenceExpression( "exception" );
            catchAll.Statements.Add( new CodeVariableDeclarationStatement( typeof( Exception ), exceptionRef.VariableName, errorHandler ) );
            catchAll.Statements.Add( new CodeMethodInvokeExpression( callInfo, nameof( IClientCallInfo.FinishWithErrors ), exceptionRef ) );
            catchAll.Statements.Add( new CodeThrowExceptionStatement( exceptionRef ) );
            errorHandler.Parameters.Add( new CodeArgumentReferenceExpression( catchAll.LocalName ) );
            if(errors != null && errors.Length > 0)
            {
                var convertExceptionMethod = GenerateClientConvertExceptionMethod( feature, name, errors );
                client.Members.Add( convertExceptionMethod );
                errorHandler.Parameters.Add( new CodeMethodReferenceExpression( null, convertExceptionMethod.Name ) );
            }
            if(responseType != null)
            {
                var responseRef = new CodeVariableReferenceExpression( nameof( response ) );
                tryStmt.TryStatements.Add( new CodeVariableDeclarationStatement( responseType, responseRef.VariableName, response ) );
                tryStmt.TryStatements.Add( new CodeMethodInvokeExpression( callInfo, nameof( IClientCallInfo.FinishSuccessful ) ) );
                tryStmt.TryStatements.Add( new CodeMethodReturnStatement( responseRef ) );
            }
            else
            {
                tryStmt.TryStatements.Add( response );
                tryStmt.TryStatements.Add( new CodeMethodInvokeExpression( callInfo, nameof( IClientCallInfo.FinishSuccessful ) ) );
                tryStmt.TryStatements.Add( new CodeMethodReturnStatement() );
            }
            return tryStmt;
        }

        private CodeMemberMethod GenerateClientConvertExceptionMethod( Feature feature, string name, string[] errors )
        {
            var convertExceptionMethod = new CodeMemberMethod
            {
                Name = "Convert" + name + "Exception",
                Attributes = MemberAttributes.Private | MemberAttributes.Static,
                ReturnType = new CodeTypeReference( typeof( Exception ) )
            };
            convertExceptionMethod.Parameters.Add( new CodeParameterDeclarationExpression( typeof( string ), "errorIdentifier" ) );
            convertExceptionMethod.Parameters.Add( new CodeParameterDeclarationExpression( typeof( string ), "errorMessage" ) );
            convertExceptionMethod.WriteDocumentation( $"Converts the error ocurred during execution of {name} to a proper exception", "The converted exception or null, if the error is not understood",
                new Dictionary<string, string>()
                {
                    {"errorIdentifier", "The identifier of the error that has happened" },
                    {"errorMessage", "The original error message from the server" }
                } );
            var identityRef = new CodeArgumentReferenceExpression( "errorIdentifier" );
            var messageRef = new CodeArgumentReferenceExpression( "errorMessage" );
            if(errors != null)
            {
                foreach(var error in errors)
                {
                    var definedError = feature.Items.OfType<FeatureDefinedExecutionError>().FirstOrDefault( e => e.Identifier == error );
                    var errorIdentifier = feature.GetFullyQualifiedIdentifier( definedError );
                    var ifError = new CodeConditionStatement
                    {
                        Condition = new CodeBinaryOperatorExpression(
                            identityRef,
                            CodeBinaryOperatorType.ValueEquality,
                            new CodePrimitiveExpression( errorIdentifier ) )
                    };
                    var exception =
                        new CodeObjectCreateExpression( _nameProvider.CreateExceptionReference( error ), messageRef );
                    ifError.TrueStatements.Add( new CodeMethodReturnStatement( exception ) );
                    convertExceptionMethod.Statements.Add( ifError );
                }
            }

            convertExceptionMethod.Statements.Add( new CodeMethodReturnStatement( new CodePrimitiveExpression() ) );
            return convertExceptionMethod;
        }

        private CodeExpression _GenerateClientGrpcInvoke( Feature feature, string methodName, CodeExpression request,
            DataTypeType responseType, string propertyName, CodeExpression callInfoRef, CodeTypeReference customResponseType )
        {
            var providerReference = new CodeTypeReferenceExpression( feature.Identifier + "Methods" );
            CodeExpression response = new CodeMethodInvokeExpression(
                new CodeFieldReferenceExpression( null, "_channel" ),
                "BlockingUnaryCall",
                new CodeFieldReferenceExpression( providerReference, methodName ),
                new CodePrimitiveExpression( null ),
                new CodeMethodInvokeExpression( callInfoRef, "ToCallOptions" ),
                request );
            return GenerateClientConvertResponse( responseType, propertyName, response, customResponseType );
        }

        private CodeExpression GenerateClientConvertResponse( DataTypeType responseType, string propertyName,
            CodeExpression response, CodeTypeReference customType )
        {
            if(responseType != null)
            {
                response = new CodePropertyReferenceExpression( response, propertyName );
                return _translationProvider.Extract( response, responseType,
                    new CodePropertyReferenceExpression( new CodeFieldReferenceExpression( null, ExecutionManagerFieldName ), nameof( IClientExecutionManager.DownloadBinaryStore ) ),
                    customType );
            }

            return response;
        }

        private void GenerateClientProperty( Feature feature, FeatureProperty featureProperty, PropertySpec spec, CodeTypeDeclaration client,
            CodeMemberMethod initLazyRequests, FeatureCommand setterCommand )
        {
            var propertyType = _nameProvider.GetMemberType( featureProperty.Identifier )
                               ?? _translationProvider.ExtractType( featureProperty.DataType, featureProperty.Identifier );
            var method = _nameProvider.GetNonstandardMethod( featureProperty );
            var lazy = spec == null || !spec.LazySpecified ? ClientGeneratorConfig.LazyUnobservableProperties : spec.Lazy;
            if (method == null && (spec == null || !spec.AsMethodSpecified || !spec.AsMethod))
            {
                GenerateClientPropertyCore(feature, featureProperty, spec, client, initLazyRequests, setterCommand, propertyType, lazy);
            }
            else
            {
                var property = new CodeMemberMethod()
                {
                    Name = spec?.Code ?? _nameProvider.GetPropertyName( featureProperty ),
                    Attributes = MemberAttributes.Public,
                    ReturnType = propertyType,
                };
                property.WriteDocumentation( spec?.Description ?? featureProperty.Description ?? $"The {featureProperty.DisplayName} property" );
                client.Members.Add( property );
                GenerateClientStaticProperty( feature, featureProperty, spec != null && spec.LazySpecified && spec.Lazy, spec?.Mapping?.ValueExpression, client, initLazyRequests, propertyType, property.Statements );
            }
        }

        private void GenerateClientPropertyCore(Feature feature, FeatureProperty featureProperty, PropertySpec spec, CodeTypeDeclaration client, CodeMemberMethod initLazyRequests, FeatureCommand setterCommand, CodeTypeReference propertyType, bool lazy)
        {
            var property = new CodeMemberProperty()
            {
                Name = spec?.Code ?? _nameProvider.GetPropertyName(featureProperty),
                Attributes = MemberAttributes.Public,
                Type = propertyType,
                HasSet = false,
                HasGet = true
            };
            property.WriteDocumentation(spec?.Description ?? featureProperty.Description ?? $"The {featureProperty.DisplayName} property");
            client.Members.Add(property);
            if (setterCommand != null)
            {
                GenerateClientPropertyCall(feature, featureProperty, spec?.Mapping?.ValueExpression, client, propertyType, property.GetStatements);
                var requestType = _nameProvider.GenerateCommandRequestType(setterCommand);
                var request = new CodeObjectCreateExpression(requestType);
                request.Parameters.Add(new CodePropertySetValueReferenceExpression());
                var requestRef = new CodeVariableReferenceExpression(nameof(request));
                string binaryParameterIdentifier = null;
                property.SetStatements.Add(new CodeVariableDeclarationStatement(requestType, requestRef.VariableName, request));
                var binaryParameter = setterCommand.Parameter.SingleOrDefault(p =>
                    ContainsBinaries(p.DataType, id => feature.Items.OfType<SiLAElement>().Single(e => e.Identifier == id).DataType));
                if (binaryParameter != null)
                {
                    binaryParameterIdentifier = feature.GetFullyQualifiedParameterIdentifier(setterCommand, binaryParameter.Identifier);
                }

                request.Parameters.Add(GenerateCreateBinaryStore(binaryParameterIdentifier));
                GenerateClientNonObservableCommandImplementation(feature, setterCommand, client, property.SetStatements, requestRef, null);
            }
            else
            {
                if (featureProperty.Observable == FeaturePropertyObservable.No)
                {
                    GenerateClientStaticProperty(feature, featureProperty, lazy, spec?.Mapping?.ValueExpression, client, initLazyRequests, propertyType, property.GetStatements);
                }
                else
                {
                    GenerateClientDynamicProperty(feature, featureProperty, client, propertyType, property);
                }
            }
        }

        private void GenerateClientStaticProperty( Feature feature, FeatureProperty featureProperty, bool lazy, Expression expression, CodeTypeDeclaration client,
            CodeMemberMethod initLazyRequests, CodeTypeReference propertyType, CodeStatementCollection property )
        {
            if(lazy)
            {
                var fieldName = "_" + featureProperty.Identifier.ToCamelCase();
                var fieldRef = new CodeFieldReferenceExpression( null, fieldName );
                var lazyType = new CodeTypeReference( typeof( Lazy<> ).FullName, propertyType );
                client.Members.Add( new CodeMemberField( lazyType, fieldName ) );
                var requestMethod = new CodeMemberMethod()
                {
                    Name = "Request" + featureProperty.Identifier,
                    Attributes = MemberAttributes.Private,
                    ReturnType = propertyType
                };
                client.Members.Add( requestMethod );
                initLazyRequests.Statements.Add( new CodeAssignStatement(
                    fieldRef,
                    new CodeObjectCreateExpression( lazyType,
                        new CodeMethodReferenceExpression( null, requestMethod.Name ) ) ) );
                property.Add(
                    new CodeMethodReturnStatement( new CodePropertyReferenceExpression( fieldRef, "Value" ) ) );
                GenerateClientPropertyCall( feature, featureProperty, expression, client, propertyType, requestMethod.Statements );
            }
            else
            {
                GenerateClientPropertyCall( feature, featureProperty, expression, client, propertyType, property );
            }
        }

        private void GenerateClientPropertyCall( Feature feature, FeatureProperty featureProperty, Expression expression, CodeTypeDeclaration client,
            CodeTypeReference propertyType, CodeStatementCollection requestMethod )
        {
            var identifier = feature.GetFullyQualifiedIdentifier( featureProperty );
            var callInfo = new CodeMethodInvokeExpression(
                new CodeFieldReferenceExpression( null, ExecutionManagerFieldName ),
                nameof( IClientExecutionManager.CreateCallOptions ),
                new CodePrimitiveExpression( identifier ) );
            var callInfoRef = new CodeVariableReferenceExpression( nameof( callInfo ) );
            requestMethod.Add(
                new CodeVariableDeclarationStatement( typeof( IClientCallInfo ), callInfoRef.VariableName, callInfo ) );
            var channelRef = new CodeFieldReferenceExpression( null, "_channel" );
            var method = new CodeMethodReferenceExpression( channelRef, nameof( IClientChannel.ReadProperty ), _nameProvider.GetPropertyResponseType( featureProperty, _translationProvider ) );
            var origin = featureProperty.Identifier;
            var propertyExpression = expression as PropertyExpression;
            if(propertyExpression != null)
            {
                origin += "." + propertyExpression.Property;
            }
            var response = GenerateClientConvertResponse(
                featureProperty.DataType, "Value",
                new CodeMethodInvokeExpression( method,
                    new CodeFieldReferenceExpression( null, ServiceNameFieldName ),
                    new CodePrimitiveExpression( featureProperty.Identifier ),
                    new CodePrimitiveExpression( feature.GetFullyQualifiedIdentifier( featureProperty ) ),
                    callInfoRef ),
                _nameProvider.GetMemberType( origin ) );
            if(propertyExpression != null)
            {
                response = new CodeObjectCreateExpression( _nameProvider.GetMemberType( featureProperty.Identifier ), response );
            }
            requestMethod.Add( GenerateClientExceptionHandling( feature, client, response, propertyType,
                featureProperty.DefinedExecutionErrors, featureProperty.Identifier, callInfoRef ) );
        }

        private void GenerateClientDynamicProperty( Feature feature, FeatureProperty featureProperty, CodeTypeDeclaration client,
            CodeTypeReference propertyType, CodeMemberProperty property )
        {
            var fieldName = "_" + featureProperty.Identifier.ToCamelCase();
            var fieldRef = new CodeFieldReferenceExpression( null, fieldName );
            client.Members.Add( new CodeMemberField( propertyType, fieldName ) );
            var updateTask = new CodeFieldReferenceExpression( null, fieldName + "UpdateTask" );
            client.Members.Add( new CodeMemberField( typeof( Task ), updateTask.FieldName ) );

            var providerReference = new CodeTypeReferenceExpression( feature.Identifier + "Methods" );
            var receiveValueMethod = GenerateClientReceiveValueMethod( featureProperty, fieldName, fieldRef, property );

            var subscriptionMethod = new CodeMethodReferenceExpression(
                new CodeFieldReferenceExpression( null, ChannelFieldName ),
                nameof( IClientChannel.SubscribeProperty ),
                _nameProvider.GetPropertyResponseType( featureProperty, _translationProvider ) );

            var tokenSource = new CodeFieldReferenceExpression( null, CancellationTokenSourceFieldName );
            var subscription = new CodeMethodInvokeExpression(
                subscriptionMethod,
                new CodeFieldReferenceExpression( null, ServiceNameFieldName ),
                new CodePrimitiveExpression( featureProperty.Identifier ),
                new CodePrimitiveExpression( feature.GetFullyQualifiedIdentifier( featureProperty ) ),
                new CodeMethodReferenceExpression( null, receiveValueMethod.Name ),
                new CodeMethodInvokeExpression( new CodeFieldReferenceExpression( null, ExecutionManagerFieldName ), nameof( IClientExecutionManager.CreateCallOptions ), new CodePrimitiveExpression( feature.GetFullyQualifiedIdentifier( featureProperty ) ) ),
                new CodePropertyReferenceExpression( tokenSource, nameof( CancellationTokenSource.Token ) ) );
            var ifSubscriptionNull = new CodeBinaryOperatorExpression( updateTask, CodeBinaryOperatorType.IdentityEquality, new CodePrimitiveExpression() );
            var ifUpdateTaskNull = new CodeConditionStatement( ifSubscriptionNull, new CodeAssignStatement( updateTask, subscription ) ); ;
            var monitor = new CodeTypeReferenceExpression( typeof( Monitor ) );
            var ifUpdateTaskNullLock = new CodeConditionStatement( ifSubscriptionNull,
                new CodeTryCatchFinallyStatement
                {
                    TryStatements =
                    {
                        new CodeMethodInvokeExpression(monitor, nameof(Monitor.Enter), tokenSource),
                        ifUpdateTaskNull
                    },
                    FinallyStatements =
                    {
                        new CodeMethodInvokeExpression(monitor, nameof(Monitor.Exit), tokenSource)
                    }
                } );

            property.GetStatements.Add( ifUpdateTaskNullLock );
            property.GetStatements.Add( new CodeMethodReturnStatement( fieldRef ) );
            client.Members.Add( receiveValueMethod );
        }

        private CodeMemberMethod GenerateClientReceiveValueMethod( FeatureProperty featureProperty, string fieldName,
            CodeFieldReferenceExpression fieldRef, CodeMemberProperty property )
        {
            var receiveValueMethod = new CodeMemberMethod
            {
                Name = "ReceiveNew" + featureProperty.Identifier,
                Attributes = MemberAttributes.Private
            };
            var parameter = new CodeParameterDeclarationExpression(
                _nameProvider.GetPropertyResponseType( featureProperty, _translationProvider ),
                fieldName.Substring( 1 ) );
            receiveValueMethod.Parameters.Add( parameter );
            var inner = new CodeVariableDeclarationStatement(
                _translationProvider.ExtractType( featureProperty.DataType, featureProperty.Identifier ),
                "inner",
                new CodeMethodInvokeExpression(
                    new CodePropertyReferenceExpression( new CodeArgumentReferenceExpression( parameter.Name ), "Value" ),
                    nameof( ISilaTransferObject<object>.Extract ),
                    new CodePropertyReferenceExpression(
                        new CodeFieldReferenceExpression( null, ExecutionManagerFieldName ),
                        nameof( IClientExecutionManager.DownloadBinaryStore ) ) ) );
            receiveValueMethod.Statements.Add( inner );
            var innerRef = new CodeVariableReferenceExpression( inner.Name );
            var ifChanged = new CodeConditionStatement(
                new CodeBinaryOperatorExpression( fieldRef, CodeBinaryOperatorType.IdentityInequality, innerRef ) );
            ifChanged.TrueStatements.Add( new CodeAssignStatement( fieldRef, innerRef ) );
            GenerateRaisePropertyChanged( ifChanged.TrueStatements, property.Name );
            receiveValueMethod.Statements.Add( ifChanged );
            return receiveValueMethod;
        }

        private static void GenerateRaisePropertyChanged( CodeStatementCollection statements, string propertyName )
        {
            var handler = new CodeVariableDeclarationStatement( typeof( PropertyChangedEventHandler ), "handler",
                new CodeFieldReferenceExpression( null, nameof( INotifyPropertyChanged.PropertyChanged ) ) );
            statements.Add( handler );
            var handlerRef = new CodeVariableReferenceExpression( handler.Name );
            var eventArgs =
                new CodeObjectCreateExpression( typeof( PropertyChangedEventArgs ),
                    new CodePrimitiveExpression( propertyName ) );
            statements.Add( new CodeConditionStatement(
                new CodeBinaryOperatorExpression( handlerRef, CodeBinaryOperatorType.IdentityInequality,
                    new CodePrimitiveExpression() ),
                new CodeExpressionStatement( new CodeMethodInvokeExpression( handlerRef, "Invoke",
                    new CodeThisReferenceExpression(), eventArgs ) ) ) );
        }

        private void GenerateClientConstructors( CodeTypeDeclaration client, bool initLazyRequests )
        {
            _loggingChannel.Debug( "Generate constructors" );
            var callInitLazyRequests = new CodeMethodInvokeExpression( null, "InitLazyRequests" );
            var constructor = new CodeConstructor() { Attributes = MemberAttributes.Public };

            var resolverParameter = new CodeParameterDeclarationExpression( typeof( IClientExecutionManager ).Name, "executionManager" );
            var channelParameter = new CodeParameterDeclarationExpression( typeof( IClientChannel ).Name, "channel" );
            var resolverField = new CodeMemberField( typeof( IClientExecutionManager ).Name, ExecutionManagerFieldName );
            var channelField = new CodeMemberField( typeof( IClientChannel ).Name, ChannelFieldName );
            client.Members.Add( resolverField );
            client.Members.Add( channelField );

            var parameterDocumentation = new Dictionary<string, string>()
            {
                {channelParameter.Name, "The channel through which calls should be executed" },
                {resolverParameter.Name, "A component to determine metadata to attach to any requests" },
            };
            constructor.WriteDocumentation( "Creates a new instance", parameters: parameterDocumentation );

            constructor.Parameters.Add( channelParameter );
            constructor.Parameters.Add( resolverParameter );
            constructor.Statements.Add( new CodeAssignStatement(
                new CodeFieldReferenceExpression( null, resolverField.Name ),
                new CodeArgumentReferenceExpression( resolverParameter.Name ) ) );
            constructor.Statements.Add( new CodeAssignStatement(
                new CodeFieldReferenceExpression( null, ChannelFieldName ),
                new CodeArgumentReferenceExpression( channelParameter.Name ) ) );
            if(initLazyRequests)
            {
                constructor.Statements.Add( callInitLazyRequests );
            }

            client.Members.Add( constructor );
        }
    }
}

#pragma warning restore S3265 // Non-flags enums should not be used in bitwise operations