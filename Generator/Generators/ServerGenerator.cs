using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Tecan.MAP.Contracts.Services.Logging;
using Tecan.Sila2.Generator.Contracts;
using Tecan.Sila2.Generator.Helper;
using Tecan.Sila2.Server;
using Metadata = Tecan.Sila2.Server.Metadata;

namespace Tecan.Sila2.Generator.Generators
{
    /// <summary>
    /// Generates the server for a feature
    /// </summary>
    [Export( typeof(IServerGenerator) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class ServerGenerator : IServerGenerator
    {
        private const string ServerFieldName = "_server";
        private const string ContextParameterName = "context";

        private readonly ICodeNameProvider _nameProvider;
        private readonly IEnumerable<IGeneratorHook> _hooks;
        private readonly ITypeTranslationProvider _translationProvider;
        private readonly ILoggingChannel _loggingChannel;
        private readonly IDependencyInjectionGenerator _diGenerator;

        /// <summary>
        /// Creates a new server generator
        /// </summary>
        /// <param name="translationProvider">A component that translates types between SiLA2 and .NET</param>
        /// <param name="nameProvider">The name provider used to generate feature names</param>
        /// <param name="loggingChannelFactory">The logging implementation</param>
        /// <param name="hooks">Generator hooks</param>
        [ImportingConstructor]
        public ServerGenerator( ITypeTranslationProvider translationProvider, ICodeNameProvider nameProvider, ILoggingChannelFactory loggingChannelFactory, IDependencyInjectionGenerator diGenerator, [ImportMany] IEnumerable<IGeneratorHook> hooks )
        {
            _translationProvider = translationProvider;
            _nameProvider = nameProvider;
            _hooks = hooks;
            _loggingChannel = loggingChannelFactory.CreateChannel( typeof(ServerGenerator) );
            _diGenerator = diGenerator;
        }


        /// <inheritdoc />
        public CodeTypeDeclaration GenerateServer( Feature feature )
        {
            _loggingChannel.Log( $"Generating feature provider for {feature.FullyQualifiedIdentifier}".ToString, Severity.Info );
            var server = new CodeTypeDeclaration( feature.Identifier + "Provider" )
            {
                Attributes = MemberAttributes.Public,
                TypeAttributes = TypeAttributes.Public,
                IsPartial = true,
                IsClass = true
            };
            server.BaseTypes.Add( nameof(IFeatureProvider) );
            var implementation = GenerateServerImplementationField( feature, server );
            server.WriteDocumentation( $"A class that exposes the {_nameProvider.CreateFeatureInterfaceReference( feature ).BaseType} interface via SiLA2" );
            var serverRef = GenerateServerGrpcServerField( server );
            GenerateServerConstructor( feature, server, implementation, serverRef );
            GenerateServerCreateServiceDefinition( feature, server );

            if( feature.Items != null )
            {

                foreach( var featureCommand in feature.Items.OfType<FeatureCommand>() )
                {
                    _loggingChannel.Log($"Generating provider for command {featureCommand.Identifier}".ToString);
                    GenerateServerCommand( feature, featureCommand, server, implementation, serverRef );
                }

                foreach( var featureProperty in feature.Items.OfType<FeatureProperty>() )
                {
                    if( featureProperty.Observable == FeaturePropertyObservable.No )
                    {
                        _loggingChannel.Log( $"Generating unobservable property for {featureProperty.Identifier}".ToString );
                        GenerateServerStaticProperty( feature, featureProperty, server, implementation, id => FindError( feature, id ) );
                    }
                    else
                    {
                        _loggingChannel.Log( $"Generating observable property for {featureProperty.Identifier}".ToString );
                        GenerateServerDynamicProperty( feature, featureProperty, server, implementation, id => FindError( feature, id ) );
                    }
                }

                foreach( var featureMetadata in feature.Items.OfType<FeatureMetadata>() )
                {
                    _loggingChannel.Log( $"Generating interceptor for {featureMetadata.Identifier}".ToString );
                    GenerateServerMetadata( featureMetadata, server, implementation );
                }
            }


            _loggingChannel.Log( "Generating provider shared code".ToString );
            GenerateServerFeatureProperty( server, feature );

            GenerateServerGetCommand( server, feature );
            GenerateServerGetProperty( server, feature );

            _diGenerator.AddDependencyInjectionRegistrations(server);

            if( _hooks != null )
            {
                foreach( var hook in _hooks )
                {
                    _loggingChannel.Log($"Executing hook {hook} after provider code generation is complete.".ToString);
                    hook?.OnServerGenerated( feature, server );
                }
            }

            return server;
        }

        private void GenerateServerGetCommand( CodeTypeDeclaration server, Feature feature )
        {
            var getCommandMethod = new CodeMemberMethod()
            {
                Name = nameof(IFeatureProvider.GetCommand),
                ReturnType = new CodeTypeReference( typeof(MethodInfo) ),
                Attributes = MemberAttributes.Final | MemberAttributes.Public
            };
            var commandIdentifier = new CodeArgumentReferenceExpression("commandIdentifier");
            getCommandMethod.Parameters.Add( new CodeParameterDeclarationExpression( typeof(string), commandIdentifier.ParameterName ) );
            var typeExpression = new CodeTypeOfExpression( _nameProvider.CreateFeatureInterfaceReference( feature ) );

            if( feature.Items != null )
            {

                foreach( var command in feature.Items.OfType<FeatureCommand>() )
                {
                    var ifStmt = new CodeConditionStatement();
                    ifStmt.Condition = new CodeBinaryOperatorExpression( commandIdentifier, CodeBinaryOperatorType.ValueEquality,
                        new CodePrimitiveExpression( feature.GetFullyQualifiedIdentifier( command ) ) );
                    ifStmt.TrueStatements.Add( new CodeMethodReturnStatement( new CodeMethodInvokeExpression( typeExpression, nameof(Type.GetMethod),
                        new CodePrimitiveExpression( _nameProvider.GetCommandName( command ) ) ) ) );
                    getCommandMethod.Statements.Add( ifStmt );
                }
            }

            getCommandMethod.WriteDocumentation( "Gets the command with the given identifier", "A method object or null, if the command is not supported",
                new Dictionary<string, string>()
                {
                    { commandIdentifier.ParameterName, "A fully qualified command identifier" }
                } );

            getCommandMethod.Statements.Add( new CodeMethodReturnStatement( new CodePrimitiveExpression() ) );
            server.Members.Add( getCommandMethod );
        }

        private void GenerateServerGetProperty( CodeTypeDeclaration server, Feature feature )
        {
            var getPropertyMethod = new CodeMemberMethod()
            {
                Name = nameof( IFeatureProvider.GetProperty ),
                ReturnType = new CodeTypeReference( typeof( PropertyInfo ) ),
                Attributes = MemberAttributes.Final | MemberAttributes.Public
            };
            var commandIdentifier = new CodeArgumentReferenceExpression( "propertyIdentifier" );
            getPropertyMethod.Parameters.Add( new CodeParameterDeclarationExpression( typeof( string ), commandIdentifier.ParameterName ) );
            var typeExpression = new CodeTypeOfExpression( _nameProvider.CreateFeatureInterfaceReference( feature ) );

            if( feature.Items != null )
            {
                foreach( var property in feature.Items.OfType<FeatureProperty>() )
                {
                    var ifStmt = new CodeConditionStatement();
                    ifStmt.Condition = new CodeBinaryOperatorExpression( commandIdentifier, CodeBinaryOperatorType.ValueEquality,
                        new CodePrimitiveExpression( feature.GetFullyQualifiedIdentifier( property ) ) );
                    ifStmt.TrueStatements.Add( new CodeMethodReturnStatement( new CodeMethodInvokeExpression( typeExpression, nameof(Type.GetProperty),
                        new CodePrimitiveExpression( _nameProvider.GetPropertyName( property ) ) ) ) );
                    getPropertyMethod.Statements.Add( ifStmt );
                }
            }

            getPropertyMethod.WriteDocumentation( "Gets the property with the given identifier", "A property object or null, if the property is not supported",
                new Dictionary<string, string>()
                {
                    { commandIdentifier.ParameterName, "A fully qualified property identifier" }
                } );

            getPropertyMethod.Statements.Add( new CodeMethodReturnStatement( new CodePrimitiveExpression() ) );
            server.Members.Add( getPropertyMethod );
        }

        private FeatureDefinedExecutionError FindError( Feature feature, string id )
        {
            return feature.Items.OfType<FeatureDefinedExecutionError>()
                .FirstOrDefault( e => e.Identifier == id );
        }

        private void GenerateServerMetadata( FeatureMetadata featureMetadata, CodeTypeDeclaration server, CodeFieldReferenceExpression implementation )
        {
            var implementationMethod = new CodeMemberMethod
            {
                Name = "GetAffectedBy" + featureMetadata.Identifier,
                Attributes = MemberAttributes.Family,
                ReturnType = new CodeTypeReference( typeof(Task<List<StringDto>>) )
            };
            implementationMethod.WriteDocumentation( $"Gets the features/commands/properties affected by the metadata {featureMetadata.Description}",
                "A list of fully qualified identifiers",
                new Dictionary<string, string>()
                {
                    { "request", "The request object (not used)" },
                    { ContextParameterName, "The request context" }
                } );
            implementationMethod.Parameters.Add(
                new CodeParameterDeclarationExpression( nameof(EmptyRequest), "request" ) );
            implementationMethod.Parameters.Add(
                new CodeParameterDeclarationExpression( typeof(ServerCallContext), ContextParameterName ) );
            var result = new CodeMethodInvokeExpression( new CodeTypeReferenceExpression( typeof(Metadata) ),
                nameof(Metadata.GetAffectedIdentifiers),
                new CodeFieldReferenceExpression( null, ServerFieldName ),
                new CodePropertyReferenceExpression( implementation, featureMetadata.Identifier ) );
            implementationMethod.Statements.Add( new CodeMethodReturnStatement( result ) );
            server.Members.Add( implementationMethod );
        }

        private void GenerateServerDynamicProperty( Feature feature, FeatureProperty featureProperty, CodeTypeDeclaration server,
            CodeFieldReferenceExpression implementation, Func<string, FeatureDefinedExecutionError> errorFinder )
        {
            var implementationMethod = new CodeMemberMethod
            {
                Name = "Subscribe" + featureProperty.Identifier,
                Attributes = MemberAttributes.Family,
                ReturnType = new CodeTypeReference( typeof(Task) )
            };
            var responseType = _nameProvider.GetPropertyResponseType( featureProperty, _translationProvider );
            implementationMethod.Parameters.Add(
                new CodeParameterDeclarationExpression( nameof(EmptyRequest), "request" ) );
            implementationMethod.Parameters.Add( new CodeParameterDeclarationExpression(
                new CodeTypeReference( typeof(IServerStreamWriter<>).FullName, responseType ), "responseStream" ) );
            implementationMethod.Parameters.Add(
                new CodeParameterDeclarationExpression( typeof(ServerCallContext), ContextParameterName ) );
            implementationMethod.WriteDocumentation( $"Subscribes to the {featureProperty.DisplayName} property", "A task that ends when the subscription ends",
                new Dictionary<string, string>()
                {
                    { "request", "The request in which the call was made" },
                    { "responseStream", "The stream in which to write new property values" },
                    { ContextParameterName, "The context in which the request is made" }
                } );
            var createResultMethod = new CodeMemberMethod
            {
                Name = "Create" + featureProperty.Identifier + "Response",
                Attributes = MemberAttributes.Private,
                ReturnType = responseType
            };
            createResultMethod.WriteDocumentation( $"Wrap the current value of {featureProperty.DisplayName} into a data transfer object",
                "A data transfer object with the current value" );

            CodeExpression actualImplementation =
                new CodePropertyReferenceExpression( implementation, _nameProvider.GetPropertyName( featureProperty ) );

            var serverRef = new CodeFieldReferenceExpression( null, ServerFieldName );
            var result = new CodeObjectCreateExpression( responseType );
            if( !(featureProperty.DataType.Item is ConstrainedType) )
            {
                result.Parameters.Add( _translationProvider.EncapsulateAsDto( actualImplementation, featureProperty.DataType, serverRef, featureProperty.Identifier ) );
            }
            else
            {
                result.Parameters.Add( actualImplementation );
                result.Parameters.Add( serverRef );
            }

            var ret = new CodeMethodReturnStatement( result );
            var statements = GenerateExceptionHandling( feature, null, featureProperty.DefinedExecutionErrors, errorFinder, createResultMethod.Statements );
            statements.Add( ret );

            var resultCall = new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression( nameof(ObservableCommandHelper) ),
                "ContinuouslySendValue",
                serverRef,
                new CodePrimitiveExpression( feature.GetFullyQualifiedIdentifier( featureProperty ) ),
                new CodeArgumentReferenceExpression( "responseStream" ),
                new CodeMethodReferenceExpression( null, createResultMethod.Name ),
                new CodePrimitiveExpression( 1000 ),
                new CodeArgumentReferenceExpression( ContextParameterName ) );
            implementationMethod.Statements.Add( new CodeMethodReturnStatement( resultCall ) );

            server.Members.Add( implementationMethod );
            server.Members.Add( createResultMethod );
        }

        private CodeFieldReferenceExpression GenerateServerGrpcServerField( CodeTypeDeclaration server )
        {
            var field = new CodeMemberField( typeof(ISiLAServer), ServerFieldName );
            server.Members.Add( field );
            return new CodeFieldReferenceExpression( null, field.Name );
        }

        private void GenerateServerFeatureProperty( CodeTypeDeclaration server, Feature feature )
        {
            var field = new CodeMemberField( typeof(Feature), "_feature" )
            {
                Attributes = MemberAttributes.Private | MemberAttributes.Static,
                InitExpression = new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression( nameof(FeatureSerializer) ),
                    "LoadFromAssembly",
                    new CodePropertyReferenceExpression( new CodeTypeOfExpression( server.Name ), "Assembly" ),
                    new CodePrimitiveExpression( feature.Identifier + ".sila.xml" ) )
            };
            server.Members.Add( field );

            var property = new CodeMemberProperty()
            {
                Name = "FeatureDefinition",
                Type = field.Type,
                Attributes = MemberAttributes.Public | MemberAttributes.Final
            };
            property.WriteDocumentation( "The feature that is exposed by this feature provider", "A feature object" );
            property.GetStatements.Add(
                new CodeMethodReturnStatement( new CodeFieldReferenceExpression( null, field.Name ) ) );
            server.Members.Add( property );
        }

        private void GenerateServerStaticProperty( Feature feature, FeatureProperty featureProperty, CodeTypeDeclaration server,
            CodeFieldReferenceExpression implementation, Func<string, FeatureDefinedExecutionError> errorFinder )
        {
            var implementationMethod = new CodeMemberMethod
            {
                Name = "Get" + featureProperty.Identifier,
                Attributes = MemberAttributes.Family
            };
            var responseType = _nameProvider.GetPropertyResponseType( featureProperty, _translationProvider );
            implementationMethod.ReturnType = new CodeTypeReference( typeof(Task<>).FullName, responseType );
            implementationMethod.Parameters.Add(
                new CodeParameterDeclarationExpression( nameof(EmptyRequest), "request" ) );
            implementationMethod.Parameters.Add(
                new CodeParameterDeclarationExpression( typeof(ServerCallContext), ContextParameterName ) );
            CodeExpression actualImplementation =
                new CodePropertyReferenceExpression( implementation, _nameProvider.GetPropertyName( featureProperty ) );

            implementationMethod.WriteDocumentation( $"Gets the current value of the {featureProperty.DisplayName} property",
                "The current value wrapped in a data transfer object", new Dictionary<string, string>()
                {
                    { "request", "The request object" },
                    { ContextParameterName, "The context in which the request is made" }
                } );

            var result = new CodeObjectCreateExpression( responseType );
            var serverRef = new CodeFieldReferenceExpression( null, ServerFieldName );
            if( !(featureProperty.DataType.Item is ConstrainedType) )
            {
                result.Parameters.Add( _translationProvider.EncapsulateAsDto( actualImplementation, featureProperty.DataType, serverRef, featureProperty.Identifier ) );
            }
            else
            {
                result.Parameters.Add( actualImplementation );
                result.Parameters.Add( serverRef );
            }

            var ret = new CodeMethodReturnStatement( new CodeMethodInvokeExpression(
                serverRef,
                nameof(ISiLAServer.CreatePropertyTask),
                new CodePrimitiveExpression( feature.GetFullyQualifiedIdentifier( featureProperty ) ),
                result,
                new CodeArgumentReferenceExpression( ContextParameterName ) ) );

            var statements = GenerateExceptionHandling( feature, null, featureProperty.DefinedExecutionErrors, errorFinder, implementationMethod.Statements );
            statements.Add( ret );
            server.Members.Add( implementationMethod );
        }

        private void GenerateServerCommand( Feature feature, FeatureCommand featureCommand, CodeTypeDeclaration server,
            CodeFieldReferenceExpression implementationRef, CodeFieldReferenceExpression serverRef )
        {
            var request =
                new CodeParameterDeclarationExpression( _nameProvider.GenerateCommandRequestType( featureCommand ),
                    "request" );
            var requestRef = new CodeArgumentReferenceExpression( request.Name );
            var context = new CodeParameterDeclarationExpression( typeof( ServerCallContext ), ContextParameterName );
            var commandMethod = new CodeMemberMethod
            {
                Name = featureCommand.Identifier,
                Attributes = MemberAttributes.Family
            };
            commandMethod.Parameters.Add( request );
            commandMethod.Parameters.Add( context );
            commandMethod.WriteDocumentation( $"Executes the {featureCommand.DisplayName} command",
                "The command response wrapped in a data transfer object",
                new Dictionary<string, string>()
                {
                    { request.Name, "A data transfer object that contains the command parameters" },
                    { context.Name, "The context in which the command is issued" }
                } );
            if( featureCommand.Observable == FeatureCommandObservable.No )
            {
                GenerateServerNonObservableCommand( feature, featureCommand, server, implementationRef, commandMethod, requestRef, serverRef );
            }
            else
            {
                GenerateServerObservableCommand( feature, featureCommand, server, implementationRef, serverRef,
                    commandMethod, requestRef, request );
            }

            server.Members.Add( commandMethod );
        }

        private void GenerateServerObservableCommand( Feature feature, FeatureCommand featureCommand, CodeTypeDeclaration server,
            CodeFieldReferenceExpression implementationRef, CodeFieldReferenceExpression serverRef, CodeMemberMethod commandMethod,
            CodeArgumentReferenceExpression requestRef, CodeParameterDeclarationExpression request )
        {
            var context = new CodeParameterDeclarationExpression( typeof( ServerCallContext ), ContextParameterName );
            var contextRef = new CodeArgumentReferenceExpression( context.Name );
            commandMethod.ReturnType = new CodeTypeReference( typeof(Task<CommandConfirmation>) );
            var startServerCall =
                new CodeMethodInvokeExpression( new CodeTypeReferenceExpression( nameof(ObservableCommandHelper) ),
                    "StartCommand" );
            startServerCall.Parameters.Add( serverRef );
            startServerCall.Parameters.Add( new CodePrimitiveExpression( feature.GetFullyQualifiedIdentifier( featureCommand ) ) );
            var method = _nameProvider.GetNonstandardMethod( featureCommand );
            if( method == null )
            {
                startServerCall.Parameters.Add( GenerateCommandImplementationCall( featureCommand, implementationRef, requestRef, serverRef ) );
            }
            else
            {
                startServerCall.Parameters.Add( GenerateCreateCommand( feature, featureCommand, method, server, implementationRef, serverRef, requestRef ) );
            }

            startServerCall.Parameters.Add( contextRef );
            commandMethod.Statements.Add( new CodeMethodReturnStatement( startServerCall ) );

            CodeExpression errorConverterRef = new CodePrimitiveExpression();
            if( featureCommand.DefinedExecutionErrors != null && featureCommand.DefinedExecutionErrors.Length > 0 )
            {
                errorConverterRef = GenerateErrorConversionMethod( feature, featureCommand, server );
            }

            server.Members.Add( GenerateServerCommandStateMethod( featureCommand, serverRef, requestRef, errorConverterRef) );

            if( featureCommand.IntermediateResponse != null && featureCommand.IntermediateResponse.Length > 0 )
            {
                GenerateServerCommandIntermediateMethod( featureCommand, server, serverRef, requestRef, errorConverterRef );
            }

            GenerateServerCommandResultMethod( featureCommand, server, serverRef, requestRef, errorConverterRef );
        }

        private CodeMethodReferenceExpression GenerateErrorConversionMethod( Feature feature, FeatureCommand featureCommand, CodeTypeDeclaration server )
        {
            var errorConversion = new CodeMemberMethod
            {
                Name = $"Convert{featureCommand.Identifier}Exceptions",
                Attributes = MemberAttributes.Private | MemberAttributes.Static,
                ReturnType = new CodeTypeReference( typeof(Exception) )
            };
            var exceptionRef = new CodeArgumentReferenceExpression("exception");
            errorConversion.Parameters.Add( new CodeParameterDeclarationExpression( typeof(Exception), exceptionRef.ParameterName ) );

            var innerStatements = GenerateExceptionHandling( feature, featureCommand, errorConversion );
            innerStatements.Add( new CodeThrowExceptionStatement( exceptionRef ) );

            server.Members.Add( errorConversion );
            return new CodeMethodReferenceExpression(null, errorConversion.Name);
        }

        private CodeExpression GenerateCreateCommand( Feature feature, FeatureCommand featureCommand, MethodInfo method, CodeTypeDeclaration server, CodeFieldReferenceExpression implementationRef, CodeFieldReferenceExpression serverRef, CodeArgumentReferenceExpression requestRef )
        {
            var commandClass = new CodeTypeDeclaration( featureCommand.Identifier + "Command" )
            {
                TypeAttributes = TypeAttributes.NestedPrivate,
                Attributes = MemberAttributes.Private,
                IsClass = true
            };
            var isTaskReturn = typeof(Task).IsAssignableFrom( method.ReturnType );
            var elementType = isTaskReturn
                ? (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>) ? method.ReturnType.GetGenericArguments()[0] : typeof(void))
                : method.ReturnType;
            var isVoid = elementType == typeof(void);
            if( isVoid )
            {
                commandClass.BaseTypes.Add( typeof(ObservableCommand) );
            }
            else
            {
                commandClass.BaseTypes.Add( new CodeTypeReference( typeof(ObservableCommand<>).FullName, new CodeTypeReference( elementType ) ) );
            }

            var constructor = new CodeConstructor
            {
                Attributes = MemberAttributes.Public
            };
            commandClass.Members.Add( constructor );
            var parentRef = commandClass.AddField( "parent", new CodeTypeReference(feature.Identifier + "Provider"), constructor );
            var commandRequest = commandClass.AddField( "request", _nameProvider.GenerateCommandRequestType( featureCommand ), constructor );

            var runMethod = new CodeMemberMethod
            {
                Name = nameof(ObservableCommand.Run),
                Attributes = MemberAttributes.Public | MemberAttributes.Override,
                ReturnType = new CodeTypeReference( method.ReturnType )
            };
            commandClass.Members.Add( runMethod );
            var commandImplementationRef = new CodeFieldReferenceExpression(parentRef, implementationRef.FieldName);
            var commandServerRef = new CodeFieldReferenceExpression(parentRef, serverRef.FieldName);
            var actualCall = GenerateCommandImplementationCall( featureCommand, commandImplementationRef, commandRequest, commandServerRef);
            if( method.GetParameters() is var parameters && parameters.Length > 0
                && parameters[parameters.Length - 1].ParameterType == typeof(CancellationToken) )
            {
                constructor.BaseConstructorArgs.Add( new CodeObjectCreateExpression( typeof(CancellationTokenSource) ) );
                actualCall.Parameters.Add( new CodePropertyReferenceExpression( null, nameof(ObservableCommand.CancellationToken) ) );
            }
            if( isTaskReturn )
            {
                runMethod.Statements.Add( new CodeMethodReturnStatement( actualCall ) );
            }
            else
            {
                var runCore = new CodeMemberMethod
                {
                    Name = "RunCore",
                    Attributes = MemberAttributes.Private,
                    ReturnType = new CodeTypeReference(method.ReturnType)
                };
                if( isVoid )
                {
                    runCore.Statements.Add( actualCall );
                    runMethod.ReturnType = new CodeTypeReference(typeof(Task));
                }
                else
                {
                    runCore.Statements.Add( new CodeMethodReturnStatement( actualCall ) );
                    runMethod.ReturnType = new CodeTypeReference(typeof(Task<>).FullName, runMethod.ReturnType);
                }

                commandClass.Members.Add( runCore );
                var startTask = new CodeMethodInvokeExpression(
                    new CodePropertyReferenceExpression( new CodeTypeReferenceExpression( typeof(Task) ), nameof(Task.Factory) ),
                    nameof(TaskFactory.StartNew),
                    new CodeMethodReferenceExpression( null, runCore.Name ),
                    new CodePropertyReferenceExpression( null, nameof(ObservableCommand.CancellationToken) ) );
                runMethod.Statements.Add( new CodeMethodReturnStatement( startTask ) );
            }

            server.Members.Add( commandClass );
            return new CodeObjectCreateExpression(commandClass.Name, new CodeThisReferenceExpression(), requestRef);
        }

        private CodeMemberMethod GenerateServerConvertResponseMethod(FeatureCommand featureCommand)
        {
            var conversionMethod = new CodeMemberMethod
            {
                Name = "Convert" + featureCommand.Identifier + "Response",
                Attributes = MemberAttributes.Private,
                ReturnType = _nameProvider.GenerateCommandResponseType(featureCommand)
            };
            var parameterType =
                _nameProvider.GetMemberType(featureCommand.Identifier + "." + featureCommand.Response[0].Identifier)
                ?? _translationProvider.ExtractType(featureCommand.Response[0].DataType, featureCommand.Identifier);
            conversionMethod.Parameters.Add(new CodeParameterDeclarationExpression(
                parameterType, "result"));
            var resultRef = new CodeArgumentReferenceExpression("result");
            var converted = GenerateClientResultConversionExpression(featureCommand.Response[0].DataType, resultRef,
                conversionMethod.ReturnType);
            conversionMethod.Statements.Add(new CodeMethodReturnStatement(converted));
            return conversionMethod;
        }

        private CodeMemberMethod GenerateServerConvertIntermediateResponseMethod(FeatureCommand featureCommand)
        {
            var conversionMethod = new CodeMemberMethod
            {
                Name = "Convert" + featureCommand.Identifier + "Intermediate",
                Attributes = MemberAttributes.Private,
                ReturnType = _nameProvider.GenerateCommandIntermediateType(featureCommand)
            };
            var parameterType = _nameProvider.GetMemberType(featureCommand.Identifier + ".Intermediate")
                                ?? _translationProvider.ExtractType(featureCommand.IntermediateResponse[0].DataType,
                                    featureCommand.Identifier + "Intermediate");
            conversionMethod.Parameters.Add(new CodeParameterDeclarationExpression(
                parameterType, "intermediate"));
            var resultRef = new CodeArgumentReferenceExpression("intermediate");
            var converted = GenerateClientResultConversionExpression(featureCommand.IntermediateResponse[0].DataType, resultRef,
                conversionMethod.ReturnType);
            conversionMethod.Statements.Add(new CodeMethodReturnStatement(converted));
            return conversionMethod;
        }

        private void GenerateServerCommandIntermediateMethod( FeatureCommand featureCommand, CodeTypeDeclaration server,
            CodeFieldReferenceExpression serverRef, CodeArgumentReferenceExpression requestRef, CodeExpression errorConverterRef )
        {
            var context = new CodeParameterDeclarationExpression( typeof( ServerCallContext ), ContextParameterName );
            var contextRef = new CodeArgumentReferenceExpression( context.Name );
            var conversionMethod = GenerateServerConvertIntermediateResponseMethod( featureCommand );
            server.Members.Add( conversionMethod );

            var commandRequest = new CodeParameterDeclarationExpression( typeof(CommandExecutionUuid), requestRef.ParameterName );
            var responseMethod = new CodeMemberMethod
            {
                Name = featureCommand.Identifier + "IntermediateResponse",
                Attributes = MemberAttributes.Private,
                ReturnType = new CodeTypeReference( typeof(Task) )
            };
            responseMethod.Parameters.Add( commandRequest );
            responseMethod.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    new CodeTypeReference( typeof(IServerStreamWriter<>).FullName, _nameProvider.GenerateCommandIntermediateType( featureCommand ) ), "responseStream" ) );
            responseMethod.Parameters.Add( context );
            responseMethod.WriteDocumentation( "Obtains a stream of intermediate results for the given command execution id",
                "A stream of intermediate responses", new Dictionary<string, string>()
                {
                    { commandRequest.Name, "The request including the command execution id" },
                    { "responseStream", "The stream in which intermediate responses should be written" },
                    { ContextParameterName, "The context in which the request is made" }
                } );
            var retrieveIntermediateMethod =
                new CodeMethodReferenceExpression( new CodeTypeReferenceExpression( nameof(ObservableCommandHelper) ),
                    nameof(ObservableCommandHelper.RetrieveIntermediateResults) );
            retrieveIntermediateMethod.TypeArguments.Add( conversionMethod.Parameters[0].Type );
            retrieveIntermediateMethod.TypeArguments.Add( conversionMethod.ReturnType );

            var waitCommandExecution =
                new CodeMethodInvokeExpression( retrieveIntermediateMethod,
                    serverRef,
                    requestRef,
                    new CodeArgumentReferenceExpression( "responseStream" ), 
                    errorConverterRef,
                    new CodeMethodReferenceExpression(null, conversionMethod.Name),
                    contextRef );
            responseMethod.Statements.Add( new CodeMethodReturnStatement( waitCommandExecution ) );
            server.Members.Add( responseMethod );
        }

        private void GenerateServerCommandResultMethod( FeatureCommand featureCommand, CodeTypeDeclaration server,
            CodeFieldReferenceExpression serverRef, CodeArgumentReferenceExpression requestRef, CodeExpression errorConverterRef )
        {
            var context = new CodeParameterDeclarationExpression( typeof( ServerCallContext ), ContextParameterName );
            var contextRef = new CodeArgumentReferenceExpression( context.Name );

            var commandRequest = new CodeParameterDeclarationExpression( typeof(CommandExecutionUuid), requestRef.ParameterName );
            var responseMethod = new CodeMemberMethod
            {
                Name = featureCommand.Identifier + "Response",
                Attributes = MemberAttributes.Private,
                ReturnType = new CodeTypeReference( typeof(Task<>).FullName,
                    _nameProvider.GenerateCommandResponseType( featureCommand ) )
            };
            responseMethod.Parameters.Add( commandRequest );
            responseMethod.Parameters.Add( context );
            responseMethod.WriteDocumentation( "Waits for the completion of the given command execution id and returns the result",
                "A task with the command result data transfer object", new Dictionary<string, string>()
                {
                    { commandRequest.Name, "The request including the command execution identifier" },
                    { context.Name, "The context in which the request is made" }
                } );
            var waitCommandMethod =
                new CodeMethodReferenceExpression( new CodeTypeReferenceExpression( nameof(ObservableCommandHelper) ),
                    nameof(ObservableCommandHelper.WaitCommandExecution) );

            var waitCommandExecution =
                new CodeMethodInvokeExpression( waitCommandMethod,
                    serverRef,
                    requestRef,
                    errorConverterRef,
                    contextRef);

            if( featureCommand.Response != null && featureCommand.Response.Length > 0 )
            {
                var conversionMethod = GenerateServerConvertResponseMethod( featureCommand );
                server.Members.Add( conversionMethod );

                waitCommandMethod.TypeArguments.Add( GetServerCommandResponseType( featureCommand ) );
                waitCommandMethod.TypeArguments.Add( _nameProvider.GenerateCommandResponseType( featureCommand ) );
                waitCommandExecution.Parameters.Add( new CodeMethodReferenceExpression( null, conversionMethod.Name ) );
            }
            responseMethod.Statements.Add( new CodeMethodReturnStatement( waitCommandExecution ) );
            server.Members.Add( responseMethod );
        }

        private CodeTypeReference GetServerCommandResponseType( FeatureCommand featureCommand )
        {
            if(featureCommand.Response.Length == 1)
            {
                var customType = _nameProvider.GetMemberType( featureCommand.Identifier + "." + featureCommand.Response[0].Identifier );
                return customType ?? _translationProvider.ExtractType( featureCommand.Response[0].DataType, featureCommand.Identifier );
            }
            else
            {
                return new CodeTypeReference( featureCommand.Identifier + "Response" );
            }
        }

        private static CodeMemberMethod GenerateServerCommandStateMethod( FeatureCommand featureCommand, CodeFieldReferenceExpression serverRef, CodeArgumentReferenceExpression requestRef, CodeExpression errorConverterRef )
        {
            var context = new CodeParameterDeclarationExpression( typeof( ServerCallContext ), ContextParameterName );
            var contextRef = new CodeArgumentReferenceExpression( context.Name );
            var stateMethod = new CodeMemberMethod()
            {
                Name = featureCommand.Identifier + "State",
                Attributes = MemberAttributes.Private,
                ReturnType = new CodeTypeReference( typeof(Task) )
            };
            var commandRequest = new CodeParameterDeclarationExpression( typeof(CommandExecutionUuid), requestRef.ParameterName );
            stateMethod.Parameters.Add( commandRequest );
            stateMethod.Parameters.Add(
                new CodeParameterDeclarationExpression( typeof(IServerStreamWriter<ExecutionState>), "responseStream" ) );
            stateMethod.Parameters.Add( context );
            stateMethod.WriteDocumentation( "Obtains a stream of states for the given command execution id",
                "A stream of intermediate responses", new Dictionary<string, string>()
                {
                    { commandRequest.Name, "The request including the command execution id" },
                    { "responseStream", "The stream in which execution states should be written" },
                    { ContextParameterName, "The context in which the request is made" }
                } );
            var queryState = new CodeMethodInvokeExpression( new CodeTypeReferenceExpression( nameof(ObservableCommandHelper) ),
                nameof(ObservableCommandHelper.QueryCommandState),
                serverRef,
                requestRef,
                new CodeArgumentReferenceExpression( "responseStream" ),
                errorConverterRef,
                contextRef );
            stateMethod.Statements.Add( new CodeMethodReturnStatement( queryState ) );
            return stateMethod;
        }

        private void GenerateServerNonObservableCommand( Feature feature, FeatureCommand featureCommand,
            CodeTypeDeclaration server, CodeFieldReferenceExpression implementationRef,
            CodeMemberMethod commandMethod,
            CodeArgumentReferenceExpression requestRef, CodeFieldReferenceExpression serverRef )
        {
            var implementationMethod =
                GenerateServerCommandInternalImplementationMethod( feature, featureCommand, implementationRef );
            server.Members.Add( implementationMethod );

            commandMethod.ReturnType =
                new CodeTypeReference( typeof(Task<>).FullName, implementationMethod.ReturnType );
            commandMethod.Statements.Add( new CodeMethodReturnStatement( new CodeMethodInvokeExpression(
                serverRef,
                nameof(ISiLAServer.InvokeCommand),
                new CodeMethodReferenceExpression( null, implementationMethod.Name ),
                requestRef,
                new CodeArgumentReferenceExpression( ContextParameterName ) ) ) );
        }

        private CodeMemberMethod GenerateServerCommandInternalImplementationMethod(
            Feature feature, FeatureCommand featureCommand,
            CodeFieldReferenceExpression implementation )
        {
            var request =
                new CodeParameterDeclarationExpression( _nameProvider.GenerateCommandRequestType( featureCommand ),
                    "request" );
            var requestRef = new CodeArgumentReferenceExpression( request.Name );
            var implementationMethod = new CodeMemberMethod
            {
                Name = featureCommand.Identifier + "Internal",
                Attributes = MemberAttributes.Private | MemberAttributes.Final
            };
            implementationMethod.ReturnType = _nameProvider.GenerateCommandResponseType( featureCommand );
            implementationMethod.Parameters.Add( request );
            CodeStatementCollection statements = GenerateExceptionHandling(feature, featureCommand, implementationMethod);
            var actualImplementation = GenerateCommandImplementationCall( featureCommand, implementation, requestRef, new CodeFieldReferenceExpression(null, ServerFieldName) );
            if( CodeGenerationHelper.IsSetterCommand( featureCommand, feature.Items.OfType<FeatureProperty>() ) )
            {
                statements.Add( new CodeAssignStatement( new CodePropertyReferenceExpression( implementation, featureCommand.Identifier.Substring( 3 ) ),
                    actualImplementation.Parameters[0] ) );
                statements.Add( new CodeMethodReturnStatement( new CodeFieldReferenceExpression(
                    new CodeTypeReferenceExpression( nameof( EmptyRequest ) ), "Instance" ) ) );
            }
            else
            {

                if( featureCommand.Response != null && featureCommand.Response.Length > 0 )
                {
                    var result = GenerateClientResultConversionExpression( featureCommand.Response[0].DataType,
                        actualImplementation, implementationMethod.ReturnType );
                    statements.Add( new CodeMethodReturnStatement( result ) );
                }
                else
                {
                    statements.Add( new CodeExpressionStatement( actualImplementation ) );
                    statements.Add( new CodeMethodReturnStatement( new CodeFieldReferenceExpression(
                        new CodeTypeReferenceExpression( nameof(EmptyRequest) ), "Instance" ) ) );
                }
            }

            return implementationMethod;
        }

        private CodeStatementCollection GenerateExceptionHandling(Feature feature, FeatureCommand featureCommand, CodeMemberMethod implementationMethod)
        {
            return GenerateExceptionHandling( feature,
                featureCommand.Parameter?.ToDictionary( e => e.Identifier.ToCamelCase(), e => feature.GetFullyQualifiedParameterIdentifier( featureCommand, e.Identifier ) ),
                featureCommand.DefinedExecutionErrors, id => FindError(feature, id), implementationMethod.Statements );
        }

        private CodeStatementCollection GenerateExceptionHandling( Feature feature, IReadOnlyDictionary<string, string> parameters, string[] executionErrors, Func<string, FeatureDefinedExecutionError> errorFinder,
            CodeStatementCollection statements )
        {
            if( (executionErrors == null || executionErrors.Length == 0) && (parameters == null || parameters.Count == 0) )
            {
                return statements;
            }
            else
            {
                var tryStmt = new CodeTryCatchFinallyStatement();
                statements.Add( tryStmt );
                var errorHandlingRef = new CodeTypeReferenceExpression( nameof(ErrorHandling) );
                if( parameters != null && parameters.Count > 0 )
                {
                    var argException = new CodeCatchClause("ex", new CodeTypeReference(typeof(ArgumentException)));
                    tryStmt.CatchClauses.Add( argException );
                    var message = new CodePropertyReferenceExpression( new CodeArgumentReferenceExpression( argException.LocalName ), nameof(Exception.Message) );
                    var parameterName = new CodePropertyReferenceExpression( message.TargetObject, nameof(ArgumentException.ParamName) );
                    foreach( var parameter in parameters )
                    {
                        var exception = new CodeMethodInvokeExpression( errorHandlingRef, nameof(ErrorHandling.CreateValidationError),
                            new CodePrimitiveExpression( parameter.Value ),
                            message );
                        argException.Statements.Add( new CodeConditionStatement(
                            new CodeBinaryOperatorExpression( parameterName, CodeBinaryOperatorType.ValueEquality, new CodePrimitiveExpression( parameter.Key ) ),
                            new CodeThrowExceptionStatement( exception ) ) );
                    }

                    argException.Statements.Add( new CodeThrowExceptionStatement(new CodeMethodInvokeExpression(errorHandlingRef, nameof(ErrorHandling.CreateUnknownValidationError), message.TargetObject)) );
                }
                if( executionErrors != null )
                {
                    foreach( var executionError in executionErrors )
                    {
                        var error = errorFinder( executionError );
                        var clause = new CodeCatchClause( "ex", _nameProvider.CreateExceptionReference( executionError ) );
                        var exception = new CodeMethodInvokeExpression(
                            errorHandlingRef, nameof(ErrorHandling.CreateExecutionError),
                            new CodePrimitiveExpression( feature.GetFullyQualifiedIdentifier( error ) ),
                            new CodePrimitiveExpression( error.Description ),
                            new CodePropertyReferenceExpression( new CodeArgumentReferenceExpression( clause.LocalName ), nameof(Exception.Message) ) );
                        clause.Statements.Add( new CodeThrowExceptionStatement( exception ) );
                        tryStmt.CatchClauses.Add( clause );
                    }
                }

                return tryStmt.TryStatements;
            }
        }

        private static CodeObjectCreateExpression GenerateClientResultConversionExpression( DataTypeType responseType,
            CodeExpression expression, CodeTypeReference resultType )
        {
            var serverRef = new CodeFieldReferenceExpression( null, ServerFieldName );
            var result = new CodeObjectCreateExpression( resultType, expression, serverRef );
            return result;
        }

        private CodeMethodInvokeExpression GenerateCommandImplementationCall( FeatureCommand featureCommand,
            CodeExpression implementation, CodeExpression requestRef, CodeExpression serverRef )
        {
            var actualImplementation =
                new CodeMethodInvokeExpression( implementation, _nameProvider.GetCommandName( featureCommand ) );
            if( featureCommand.Parameter != null )
            {
                foreach( var parameter in featureCommand.Parameter )
                {
                    var propertyName = parameter.Identifier;
                    if( parameter.Identifier == nameof(ISilaRequestObject.CommandIdentifier) )
                    {
                        propertyName += "_";
                    }
                    var typeRef = _nameProvider?.GetMemberType( featureCommand.Identifier + "." + parameter.Identifier );

                    actualImplementation.Parameters.Add( _translationProvider.Extract( new CodePropertyReferenceExpression( requestRef, propertyName ), parameter.DataType, serverRef, typeRef ) );
                }
            }

            return actualImplementation;
        }

        private void GenerateServerCreateServiceDefinition( Feature feature, CodeTypeDeclaration server )
        {
            var createServiceDefinition = new CodeMemberMethod
            {
                Name = "CreateServiceDefinition",
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                ReturnType = new CodeTypeReference( typeof(ServerServiceDefinition) ),
            };
            createServiceDefinition.WriteDocumentation( "Creates a service definition for this service provider",
                "An object that describes which services are offered by this instance" );
            var builderDeclaration = new CodeVariableDeclarationStatement()
            {
                Name = "builder",
                Type = new CodeTypeReference( typeof(ServerServiceDefinition.Builder) ),
                InitExpression =
                    new CodeMethodInvokeExpression( new CodeTypeReferenceExpression( typeof(ServerServiceDefinition) ),
                        "CreateBuilder" )
            };
            createServiceDefinition.Statements.Add( builderDeclaration );
            var builderRef = new CodeVariableReferenceExpression( builderDeclaration.Name );

            var providerReference = new CodeTypeReferenceExpression(feature.Identifier + "Methods");

            if( feature.Items != null )
            {

                foreach( var featureCommand in feature.Items.OfType<FeatureCommand>() )
                {
                    if( featureCommand.Observable == FeatureCommandObservable.No )
                    {
                        createServiceDefinition.Statements.Add( new CodeMethodInvokeExpression(
                            builderRef, "AddMethod",
                            new CodeFieldReferenceExpression( providerReference, featureCommand.Identifier + "Method" ),
                            new CodeMethodReferenceExpression( null, featureCommand.Identifier ) ) );
                    }
                    else
                    {
                        createServiceDefinition.Statements.Add( new CodeMethodInvokeExpression(
                            builderRef, "AddMethod",
                            new CodeFieldReferenceExpression( providerReference, featureCommand.Identifier + "Method" ),
                            new CodeMethodReferenceExpression( null, featureCommand.Identifier ) ) );
                        createServiceDefinition.Statements.Add( new CodeMethodInvokeExpression(
                            builderRef, "AddMethod",
                            new CodeFieldReferenceExpression( providerReference, featureCommand.Identifier + "StateMethod" ),
                            new CodeMethodReferenceExpression( null, featureCommand.Identifier + "State" ) ) );
                        if( featureCommand.IntermediateResponse != null && featureCommand.IntermediateResponse.Length > 0 )
                        {
                            createServiceDefinition.Statements.Add( new CodeMethodInvokeExpression(
                                builderRef, "AddMethod",
                                new CodeFieldReferenceExpression( providerReference, featureCommand.Identifier + "IntermediateMethod" ),
                                new CodeMethodReferenceExpression( null, featureCommand.Identifier + "IntermediateResponse" ) ) );
                        }

                        createServiceDefinition.Statements.Add( new CodeMethodInvokeExpression(
                            builderRef, "AddMethod",
                            new CodeFieldReferenceExpression( providerReference, featureCommand.Identifier + "ResponseMethod" ),
                            new CodeMethodReferenceExpression( null, featureCommand.Identifier + "Response" ) ) );
                    }
                }

                foreach( var featureProperty in feature.Items.OfType<FeatureProperty>() )
                {
                    if( featureProperty.Observable == FeaturePropertyObservable.No )
                    {
                        createServiceDefinition.Statements.Add( new CodeMethodInvokeExpression(
                            builderRef, "AddMethod",
                            new CodeFieldReferenceExpression( providerReference, "Get" + featureProperty.Identifier + "Method" ),
                            new CodeMethodReferenceExpression( null, "Get" + featureProperty.Identifier ) ) );
                    }
                    else
                    {
                        createServiceDefinition.Statements.Add( new CodeMethodInvokeExpression(
                            builderRef, "AddMethod",
                            new CodeFieldReferenceExpression( providerReference, "Subscribe" + featureProperty.Identifier + "Method" ),
                            new CodeMethodReferenceExpression( null, "Subscribe" + featureProperty.Identifier ) ) );
                    }
                }

                foreach( var featureMetadata in feature.Items.OfType<FeatureMetadata>() )
                {
                    createServiceDefinition.Statements.Add( new CodeMethodInvokeExpression(
                        builderRef, "AddMethod",
                        new CodeFieldReferenceExpression( providerReference, "GetAffectedBy" + featureMetadata.Identifier + "Method" ),
                        new CodeMethodReferenceExpression( null, "GetAffectedBy" + featureMetadata.Identifier ) ) );
                }
            }

            createServiceDefinition.Statements.Add(
                new CodeMethodReturnStatement( new CodeMethodInvokeExpression( builderRef, "Build" ) ) );
            server.Members.Add( createServiceDefinition );
        }

        private void GenerateServerConstructor( Feature feature, CodeTypeDeclaration server,
            CodeFieldReferenceExpression implementation, CodeFieldReferenceExpression innerServer )
        {
            var constructor = new CodeConstructor
            {
                Attributes = MemberAttributes.Public,
                Parameters =
                {
                    new CodeParameterDeclarationExpression( _nameProvider.CreateFeatureInterfaceReference( feature ),
                        nameof(implementation) ),
                    new CodeParameterDeclarationExpression( typeof(ISiLAServer), nameof(server) )
                },
                Statements =
                {
                    new CodeAssignStatement( implementation,
                        new CodeArgumentReferenceExpression( nameof(implementation) ) ),
                    new CodeAssignStatement( innerServer,
                        new CodeArgumentReferenceExpression( nameof(server) ) )
                }
            };
            constructor.WriteDocumentation( "Creates a new instance", parameters: new Dictionary<string, string>()
            {
                { nameof(implementation), "The implementation to exported through SiLA2" },
                { nameof(server), "The SiLA2 server instance through which the implementation shall be exported" }
            } );
            server.Members.Add( constructor );
        }

        private CodeFieldReferenceExpression GenerateServerImplementationField( Feature feature,
            CodeTypeDeclaration server )
        {
            var field = new CodeMemberField( _nameProvider.CreateFeatureInterfaceReference( feature ), "_implementation" );
            server.Members.Add( field );
            return new CodeFieldReferenceExpression( null, field.Name );
        }

        public CodeTypeDeclaration GenerateMetadataInterceptorBase( Feature feature, FeatureMetadata metadata )
        {
            var metadataInterceptor = new CodeTypeDeclaration
            {
                Name = metadata.Identifier + "InterceptorBase",
                TypeAttributes = TypeAttributes.Abstract | TypeAttributes.Public,
                IsClass = true,
                IsPartial = true
            };
            metadataInterceptor.WriteDocumentation( $"An abstract base class to support the {metadata.DisplayName} metadata" );
            metadataInterceptor.BaseTypes.Add( typeof(IRequestInterceptor) );
            metadataInterceptor.Members.Add( CreateAbstractProperty( nameof(IRequestInterceptor.AppliesToCommands), typeof(bool) )
                .WriteDocumentation( "Gets whether the metadata applies to commands", "True, if the metadata is applicable to commands, otherwise false." ) );
            metadataInterceptor.Members.Add( CreateAbstractProperty( nameof(IRequestInterceptor.AppliesToProperties), typeof(bool) )
                .WriteDocumentation( "Gets whether the metadata applies to properties", "True, if the metadata is applicable to properties, otherwise false." ) );
            metadataInterceptor.Members.Add( CreateAbstractProperty( nameof(IRequestInterceptor.Priority), typeof(int) )
                .WriteDocumentation( "Gets the priority for the metadata", "A relative priority." ) );
            metadataInterceptor.Members.Add( (new CodeMemberMethod
            {
                Name = nameof(IRequestInterceptor.IsInterceptRequired),
                ReturnType = new CodeTypeReference( typeof(bool) ),
                Attributes = MemberAttributes.Public | MemberAttributes.Abstract,
                Parameters =
                {
                    new CodeParameterDeclarationExpression( typeof(Feature).Name, "feature" ),
                    new CodeParameterDeclarationExpression( typeof(string), "commandIdentifier" )
                }
            }).WriteDocumentation( "Decides whether the interceptor is required for the given command", "True, if the interceptor should be applied",
                new Dictionary<string, string>()
                {
                    { "feature", "The feature that contains the command or property" },
                    { "commandIdentifier", "The fully qualified identifier of the command or property in question" }
                } ) );
            var metadataType = _translationProvider.ExtractType(  metadata.DataType, metadata.Identifier );
            metadataInterceptor.Members.Add( (new CodeMemberMethod
            {
                Name = nameof(IRequestInterceptor.Intercept),
                ReturnType = new CodeTypeReference( typeof(IRequestInterception) ),
                Attributes = MemberAttributes.Public | MemberAttributes.Abstract,
                Parameters =
                {
                    new CodeParameterDeclarationExpression( typeof(string), "commandIdentifier" ),
                    new CodeParameterDeclarationExpression( metadataType,
                        char.ToLowerInvariant( metadata.Identifier[0] ) + metadata.Identifier.Substring( 1 ) )
                }
            }).WriteDocumentation( "Intercepts the call to the given command", "A request interception or null, if this is not necessary",
                new Dictionary<string, string>()
                {
                    { "commandIdentifier", "The fully qualified identifier of the command or property in question" },
                    { char.ToLowerInvariant( metadata.Identifier[0] ) + metadata.Identifier.Substring( 1 ), "The parsed metadata" }
                } ) );
            metadataInterceptor.Members.Add( GenerateMetadataInterceptMethod( feature, metadata, null ) );
            return metadataInterceptor;
        }

        private CodeMemberMethod GenerateMetadataInterceptMethod( Feature feature, FeatureMetadata metadata, Action<string, StructureType> structHandler )
        {
            var interceptMethod = new CodeMemberMethod
            {
                Name = nameof(IRequestInterceptor.Intercept),
                ReturnType = new CodeTypeReference( typeof(IRequestInterception) ),
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                Parameters =
                {
                    new CodeParameterDeclarationExpression( typeof(string), "commandIdentifier" ),
                    new CodeParameterDeclarationExpression( typeof(ISiLAServer), "server" ),
                    new CodeParameterDeclarationExpression( typeof(ServerCallContext), ContextParameterName )
                }
            };
            var extractMetadata = new CodeMethodInvokeExpression(
                new CodeMethodReferenceExpression( new CodeTypeReferenceExpression( typeof(Metadata) ), nameof(Metadata.Extract),
                    _translationProvider.GetDtoTypeReference( metadata.DataType, metadata.Identifier, structHandler ) ),
                new CodeArgumentReferenceExpression( ContextParameterName ),
                new CodePrimitiveExpression( feature.GetFullyQualifiedIdentifier( metadata ) ) );
            var statements = GenerateExceptionHandling( feature, null, metadata.DefinedExecutionErrors, id => FindError( feature, id ), interceptMethod.Statements );
            statements.Add( new CodeMethodReturnStatement(
                new CodeMethodInvokeExpression(
                    null,
                    interceptMethod.Name,
                    new CodeArgumentReferenceExpression( "commandIdentifier" ),
                    new CodeMethodInvokeExpression(
                        extractMetadata,
                        "Extract",
                        new CodeArgumentReferenceExpression( "server" ) ) ) ) );
            interceptMethod.WriteDocumentation( "Intercepts the call to the given command", "A request interception or null, if this is not necessary",
                new Dictionary<string, string>()
                {
                    { "commandIdentifier", "The fully qualified identifier of the command or property in question" },
                    { "server", "The server that should execute the request" },
                    { ContextParameterName, "The context in which the request is made" }
                } );
            return interceptMethod;
        }

        private CodeMemberProperty CreateAbstractProperty( string name, Type type )
        {
            return new CodeMemberProperty
            {
                Name = name,
                Attributes = MemberAttributes.Public | MemberAttributes.Abstract,
                HasGet = true,
                HasSet = false,
                Type = new CodeTypeReference( type )
            };
        }
    }
}