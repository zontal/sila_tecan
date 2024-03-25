using Common.Logging;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tecan.Sila2.Generator.Contracts;
using Tecan.Sila2.Generator.Helper;
using Tecan.Sila2.Server;
using Metadata = Tecan.Sila2.Server.Metadata;

#pragma warning disable S3265 // Non-flags enums should not be used in bitwise operations

namespace Tecan.Sila2.Generator.Generators
{
    /// <summary>
    /// Generates the server for a feature
    /// </summary>
    [Export( typeof( IServerGenerator ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class ServerGenerator : IServerGenerator
    {
        private const string ServerFieldName = "_server";

        private readonly ICodeNameProvider _nameProvider;
        private readonly IEnumerable<IGeneratorHook> _hooks;
        private readonly ITypeTranslationProvider _translationProvider;
        private readonly ILog _loggingChannel = LogManager.GetLogger<ServerGenerator>();
        private readonly IDependencyInjectionGenerator _diGenerator;
        private readonly IGeneratorConfigSource _generatorConfigSource;

        /// <summary>
        /// Creates a new server generator
        /// </summary>
        /// <param name="translationProvider">A component that translates types between SiLA2 and .NET</param>
        /// <param name="nameProvider">The name provider used to generate feature names</param>
        /// <param name="diGenerator">A component the generates the necessary DI registration</param>
        /// <param name="hooks">Generator hooks</param>
        /// <param name="configSource">A repository of configuration sources</param>
        [ImportingConstructor]
        public ServerGenerator( ITypeTranslationProvider translationProvider, ICodeNameProvider nameProvider, IDependencyInjectionGenerator diGenerator, [ImportMany] IEnumerable<IGeneratorHook> hooks, IGeneratorConfigSource configSource )
        {
            _translationProvider = translationProvider;
            _nameProvider = nameProvider;
            _hooks = hooks;
            _diGenerator = diGenerator;
            _generatorConfigSource = configSource;
        }



        /// <inheritdoc />
        public CodeCompileUnit GenerateServer( Feature feature, string ns )
        {
            _loggingChannel.Info( $"Generating server for {feature.FullyQualifiedIdentifier}" );
            var unit = new CodeCompileUnit();
            var nSpace = new CodeNamespace( ns );
            unit.Namespaces.Add( nSpace );

            var serverClass = GenerateServerClass( feature );
            nSpace.Types.Add( serverClass );

            if(feature.Items != null)
            {
                foreach(var metadata in feature.Items.OfType<FeatureMetadata>())
                {
                    nSpace.Types.Add( GenerateMetadataInterceptorBase( feature, metadata ) );
                }
            }

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
                    generatorHook?.OnServerGenerated( feature, serverClass, unit );
                }
            }

            return unit;
        }


        /// <inheritdoc />
        public CodeTypeDeclaration GenerateServerClass( Feature feature )
        {
            _loggingChannel.Info( $"Generating feature provider for {feature.FullyQualifiedIdentifier}" );
            var server = new CodeTypeDeclaration( feature.Identifier + "Provider" )
            {
                Attributes = MemberAttributes.Public,
                TypeAttributes = TypeAttributes.Public,
                IsPartial = true,
                IsClass = true
            };
            server.BaseTypes.Add( nameof( IFeatureProvider ) );
            var implementation = GenerateServerImplementationField( feature, server );
            server.WriteDocumentation( $"A class that exposes the {_nameProvider.CreateFeatureInterfaceReference( feature ).BaseType} interface via SiLA2" );
            var serverRef = GenerateServerGrpcServerField( server );
            GenerateServerConstructor( feature, server, implementation, serverRef );
            GenerateServerRegister( feature, server, implementation );

            var spec = _generatorConfigSource.GetFeatureSpec( feature.Identifier );

            if(feature.Items != null)
            {

                foreach(var featureCommand in feature.Items.OfType<FeatureCommand>())
                {
                    _loggingChannel.Debug( $"Generating provider for command {featureCommand.Identifier}" );
                    string commandPath = null;
                    var commandSpec = spec?.FindCommandSpec( featureCommand.Identifier, out commandPath );
                    GenerateServerCommand( feature, featureCommand, commandSpec, server, Follow( implementation, commandPath ), serverRef );
                }

                foreach(var featureProperty in feature.Items.OfType<FeatureProperty>())
                {
                    _loggingChannel.Debug( $"Generating property for {featureProperty.Identifier}" );
                    string propertyPath = null;
                    var propertySpec = spec?.FindPropertySpec( featureProperty.Identifier, out propertyPath );
                    GenerateServerProperty( feature, featureProperty, propertySpec, server, Follow( implementation, propertyPath ), id => FindError( feature, id ) );
                }
            }
            else
            {
                _loggingChannel.Warn( $"The feature {feature.Identifier} has no items." );
            }


            _loggingChannel.Debug( "Generating provider shared code" );
            GenerateServerFeatureProperty( server, feature );

            GenerateServerGetCommand( server, feature );
            GenerateServerGetProperty( server, feature );

            _diGenerator.AddDependencyInjectionRegistrations( server );

            return server;
        }

        private CodeExpression Follow( CodeExpression expression, string path )
        {
            var result = expression;

            if(!string.IsNullOrEmpty( path ))
            {
                var segments = path.Split( new[] { '.' }, StringSplitOptions.RemoveEmptyEntries );
                foreach(var seg in segments)
                {
                    result = new CodePropertyReferenceExpression( result, seg );
                }
            }

            return result;
        }

        private void GenerateServerGetCommand( CodeTypeDeclaration server, Feature feature )
        {
            var getCommandMethod = new CodeMemberMethod()
            {
                Name = nameof( IFeatureProvider.GetCommand ),
                ReturnType = new CodeTypeReference( typeof( MethodInfo ) ),
                Attributes = MemberAttributes.Final | MemberAttributes.Public
            };
            var commandIdentifier = new CodeArgumentReferenceExpression( "commandIdentifier" );
            getCommandMethod.Parameters.Add( new CodeParameterDeclarationExpression( typeof( string ), commandIdentifier.ParameterName ) );
            var typeExpression = new CodeTypeOfExpression( _nameProvider.CreateFeatureInterfaceReference( feature ) );

            if(feature.Items != null)
            {

                foreach(var command in feature.Items.OfType<FeatureCommand>())
                {
                    var ifStmt = new CodeConditionStatement
                    {
                        Condition = new CodeBinaryOperatorExpression( commandIdentifier, CodeBinaryOperatorType.ValueEquality,
                            new CodePrimitiveExpression( feature.GetFullyQualifiedIdentifier( command ) ) )
                    };
                    ifStmt.TrueStatements.Add( new CodeMethodReturnStatement( new CodeMethodInvokeExpression( typeExpression, nameof( Type.GetMethod ),
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

            if(feature.Items != null)
            {
                foreach(var property in feature.Items.OfType<FeatureProperty>())
                {
                    var ifStmt = new CodeConditionStatement
                    {
                        Condition = new CodeBinaryOperatorExpression( commandIdentifier, CodeBinaryOperatorType.ValueEquality,
                        new CodePrimitiveExpression( feature.GetFullyQualifiedIdentifier( property ) ) )
                    };
                    ifStmt.TrueStatements.Add( new CodeMethodReturnStatement( new CodeMethodInvokeExpression( typeExpression, nameof( Type.GetProperty ),
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

        private static FeatureDefinedExecutionError FindError( Feature feature, string id )
        {
            return feature.Items.OfType<FeatureDefinedExecutionError>()
                .FirstOrDefault( e => e.Identifier == id );
        }

        private static CodeFieldReferenceExpression GenerateServerGrpcServerField( CodeTypeDeclaration server )
        {
            var field = new CodeMemberField( typeof( ISiLAServer ), ServerFieldName );
            server.Members.Add( field );
            return new CodeFieldReferenceExpression( null, field.Name );
        }

        private static void GenerateServerFeatureProperty( CodeTypeDeclaration server, Feature feature )
        {
            var field = new CodeMemberField( typeof( Feature ), "_feature" )
            {
                Attributes = MemberAttributes.Private | MemberAttributes.Static,
                InitExpression = new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression( nameof( FeatureSerializer ) ),
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

        private void GenerateServerProperty( Feature feature, FeatureProperty featureProperty, PropertySpec spec, CodeTypeDeclaration server,
            CodeExpression implementation, Func<string, FeatureDefinedExecutionError> errorFinder )
        {
            var implementationMethod = new CodeMemberMethod
            {
                Name = "Get" + featureProperty.Identifier,
                Attributes = MemberAttributes.Family,
                ReturnType = _nameProvider.GetPropertyResponseType( featureProperty, _translationProvider )
            };
            var method = _nameProvider.GetNonstandardMethod( featureProperty );
            var serverRef = new CodeFieldReferenceExpression( null, ServerFieldName );
            var statements = GenerateExceptionHandling( feature, null, featureProperty.DefinedExecutionErrors, errorFinder, implementationMethod.Statements, serverRef );
            CodeExpression actualImplementation;
            if(method != null)
            {
                actualImplementation =
                    new CodeMethodInvokeExpression( implementation, method.Name );
            }
            else
            {
                actualImplementation =
                    new CodePropertyReferenceExpression( implementation, _nameProvider.GetPropertyName( featureProperty ) );
            }

            actualImplementation = FollowExpression( actualImplementation, spec?.Mapping?.ValueExpression );

            implementationMethod.WriteDocumentation( $"Gets the current value of the {featureProperty.DisplayName} property",
                "The current value wrapped in a data transfer object" );

            var result = new CodeObjectCreateExpression( implementationMethod.ReturnType );
            if(!(featureProperty.DataType.Item is ConstrainedType))
            {
                result.Parameters.Add( _translationProvider.EncapsulateAsDto( actualImplementation, featureProperty.DataType, serverRef, featureProperty.Identifier ) );
            }
            else
            {
                result.Parameters.Add( actualImplementation );
                result.Parameters.Add( serverRef );
            }

            var ret = new CodeMethodReturnStatement( result );

            statements.Add( ret );
            server.Members.Add( implementationMethod );
        }

        private CodeExpression FollowExpressionReverse( CodeExpression expression, Expression toFollow, string origin )
        {
            switch(toFollow)
            {
                case PropertyExpression property:
                    var type = _nameProvider.GetMemberType( origin );
                    if(string.IsNullOrEmpty( property.CreateMethod ))
                    {
                        return new CodeObjectCreateExpression( type, expression );
                    }
                    else
                    {
                        return new CodeMethodInvokeExpression( new CodeTypeReferenceExpression( type ), property.CreateMethod, expression );
                    }
                case FormatExpression:
                    _loggingChannel.Error( "Format Expressions on inputs are currently not supported." );
                    return expression;
                default:
                    return expression;
            }
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

        private void GenerateServerCommand( Feature feature, FeatureCommand featureCommand, CommandSpec spec, CodeTypeDeclaration server,
            CodeExpression implementationRef, CodeFieldReferenceExpression serverRef )
        {
            var request =
                new CodeParameterDeclarationExpression( _nameProvider.GenerateCommandRequestType( featureCommand ),
                    "request" );
            var requestRef = new CodeArgumentReferenceExpression( request.Name );
            var commandMethod = new CodeMemberMethod
            {
                Name = featureCommand.Identifier,
                Attributes = MemberAttributes.Family
            };
            commandMethod.Parameters.Add( request );
            commandMethod.WriteDocumentation( $"Executes the {featureCommand.DisplayName} command",
                "The command response wrapped in a data transfer object",
                new Dictionary<string, string>()
                {
                    { request.Name, "A data transfer object that contains the command parameters" }
                } );
            if(featureCommand.Observable == FeatureCommandObservable.No)
            {
                GenerateServerNonObservableCommand( feature, featureCommand, spec, implementationRef, commandMethod, requestRef, serverRef );
            }
            else
            {
                GenerateServerObservableCommand( feature, featureCommand, spec, server, implementationRef, serverRef,
                    commandMethod, requestRef );
            }

            server.Members.Add( commandMethod );
        }

        private void GenerateServerObservableCommand( Feature feature, FeatureCommand featureCommand, CommandSpec spec, CodeTypeDeclaration server,
            CodeExpression implementationRef, CodeFieldReferenceExpression serverRef, CodeMemberMethod commandMethod,
            CodeArgumentReferenceExpression requestRef )
        {
            commandMethod.ReturnType = _nameProvider.GetObservableCommandReturnType( featureCommand, _translationProvider );

            var method = _nameProvider.GetNonstandardMethod( featureCommand );
            CodeExpression actualCall;
            if(method == null)
            {
                actualCall = GenerateCommandImplementationCall( feature, featureCommand, spec, implementationRef, requestRef, serverRef );
            }
            else if(method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof( IObservable<> ))
            {
                actualCall = new CodeObjectCreateExpression(
                    new CodeTypeReference( typeof( ObservableRxCommand<> ).Name, new CodeTypeReference( method.ReturnType.GetGenericArguments()[0] ) ),
                    GenerateCommandImplementationCall( feature, featureCommand, spec, implementationRef, requestRef, serverRef ) );
            }
            else
            {
                actualCall = GenerateCreateCommand( feature, featureCommand, spec, method, server, implementationRef, serverRef, requestRef );
            }
            commandMethod.Statements.Add( new CodeMethodInvokeExpression( requestRef, nameof( Argument.Validate ) ) );
            commandMethod.Statements.Add( new CodeMethodReturnStatement( actualCall ) );

            if(featureCommand.IntermediateResponse != null && featureCommand.IntermediateResponse.Length > 0)
            {
                var convertIntermediateMethod = GenerateServerConvertIntermediateResponseMethod( featureCommand );
                server.Members.Add( convertIntermediateMethod );
            }

            if(featureCommand.Response != null && featureCommand.Response.Length > 0)
            {
                var convertResponse = GenerateServerConvertResponseMethod( featureCommand );
                server.Members.Add( convertResponse );
            }
        }

        private CodeMethodReferenceExpression GenerateErrorConversionMethod( Feature feature, FeatureCommand featureCommand, CodeTypeDeclaration server )
        {
            var errorConversion = new CodeMemberMethod
            {
                Name = $"Convert{featureCommand.Identifier}Exceptions",
                Attributes = MemberAttributes.Private,
                ReturnType = new CodeTypeReference( typeof( Exception ) )
            };
            var exceptionRef = new CodeArgumentReferenceExpression( "exception" );
            errorConversion.Parameters.Add( new CodeParameterDeclarationExpression( typeof( Exception ), exceptionRef.ParameterName ) );

            var innerStatements = GenerateExceptionHandling( feature, featureCommand, errorConversion, new CodeFieldReferenceExpression( null, ServerFieldName ) );
            innerStatements.Add( new CodeThrowExceptionStatement( exceptionRef ) );

            server.Members.Add( errorConversion );
            return new CodeMethodReferenceExpression( null, errorConversion.Name );
        }

        private CodeExpression GenerateCreateCommand( Feature feature, FeatureCommand featureCommand, CommandSpec spec, MethodInfo method, CodeTypeDeclaration server, CodeExpression implementationRef, CodeFieldReferenceExpression serverRef, CodeArgumentReferenceExpression requestRef )
        {
            var commandClass = new CodeTypeDeclaration( featureCommand.Identifier + "Command" )
            {
                TypeAttributes = TypeAttributes.NestedPrivate,
                Attributes = MemberAttributes.Private,
                IsClass = true
            };
            var isTaskReturn = typeof( Task ).IsAssignableFrom( method.ReturnType );
            var elementType = isTaskReturn
                ? (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof( Task<> ) ? method.ReturnType.GetGenericArguments()[0] : typeof( void ))
                : method.ReturnType;
            var isVoid = elementType == typeof( void );
            if(isVoid)
            {
                commandClass.BaseTypes.Add( typeof( ObservableCommand ) );
            }
            else
            {
                commandClass.BaseTypes.Add( new CodeTypeReference( typeof( ObservableCommand<> ).FullName, new CodeTypeReference( elementType ) ) );
            }

            var constructor = new CodeConstructor
            {
                Attributes = MemberAttributes.Public
            };
            commandClass.Members.Add( constructor );
            var parentRef = commandClass.AddField( "parent", new CodeTypeReference( feature.Identifier + "Provider" ), constructor );
            var commandRequest = commandClass.AddField( "request", _nameProvider.GenerateCommandRequestType( featureCommand ), constructor );

            var runMethod = new CodeMemberMethod
            {
                Name = nameof( ObservableCommand.Run ),
                Attributes = MemberAttributes.Public | MemberAttributes.Override,
                ReturnType = new CodeTypeReference( method.ReturnType )
            };
            commandClass.Members.Add( runMethod );
            var commandImplementationRef = new CodeFieldReferenceExpression( parentRef, "_implementation" );
            var commandServerRef = new CodeFieldReferenceExpression( parentRef, serverRef.FieldName );
            var actualCall = GenerateCommandImplementationCall( feature, featureCommand, spec, commandImplementationRef, commandRequest, commandServerRef );
            if(method.GetParameters() is var parameters && parameters.Length > 0
                && parameters[parameters.Length - 1].ParameterType == typeof( CancellationToken ))
            {
                constructor.BaseConstructorArgs.Add( new CodeObjectCreateExpression( typeof( CancellationTokenSource ) ) );
                actualCall.Parameters.Add( new CodePropertyReferenceExpression( null, nameof( ObservableCommand.CancellationToken ) ) );
            }
            if(isTaskReturn)
            {
                runMethod.Statements.Add( new CodeMethodReturnStatement( actualCall ) );
            }
            else
            {
                var runCore = new CodeMemberMethod
                {
                    Name = "RunCore",
                    Attributes = MemberAttributes.Private,
                    ReturnType = new CodeTypeReference( method.ReturnType )
                };
                if(isVoid)
                {
                    runCore.Statements.Add( actualCall );
                    runMethod.ReturnType = new CodeTypeReference( typeof( Task ) );
                }
                else
                {
                    runCore.Statements.Add( new CodeMethodReturnStatement( actualCall ) );
                    runMethod.ReturnType = new CodeTypeReference( typeof( Task<> ).FullName, runMethod.ReturnType );
                }

                commandClass.Members.Add( runCore );
                var startTask = new CodeMethodInvokeExpression(
                    new CodePropertyReferenceExpression( new CodeTypeReferenceExpression( typeof( Task ) ), nameof( Task.Factory ) ),
                    nameof( TaskFactory.StartNew ),
                    new CodeMethodReferenceExpression( null, runCore.Name ),
                    new CodePropertyReferenceExpression( null, nameof( ObservableCommand.CancellationToken ) ) );
                runMethod.Statements.Add( new CodeMethodReturnStatement( startTask ) );
            }

            server.Members.Add( commandClass );
            return new CodeObjectCreateExpression( commandClass.Name, new CodeThisReferenceExpression(), requestRef );
        }

        private CodeMemberMethod GenerateServerConvertResponseMethod( FeatureCommand featureCommand )
        {
            var conversionMethod = new CodeMemberMethod
            {
                Name = "Convert" + featureCommand.Identifier + "Response",
                Attributes = MemberAttributes.Private,
                ReturnType = _nameProvider.GenerateCommandResponseType( featureCommand )
            };
            var parameterType =
                _nameProvider.GetMemberType( featureCommand.Identifier + "." + featureCommand.Response[0].Identifier )
                ?? _translationProvider.ExtractType( featureCommand.Response[0].DataType, featureCommand.Identifier );
            conversionMethod.Parameters.Add( new CodeParameterDeclarationExpression(
                parameterType, "result" ) );
            var resultRef = new CodeArgumentReferenceExpression( "result" );
            var converted = GenerateClientResultConversionExpression( resultRef,
                conversionMethod.ReturnType );
            conversionMethod.Statements.Add( new CodeMethodReturnStatement( converted ) );
            return conversionMethod;
        }

        private CodeMemberMethod GenerateServerConvertIntermediateResponseMethod( FeatureCommand featureCommand )
        {
            var conversionMethod = new CodeMemberMethod
            {
                Name = "Convert" + featureCommand.Identifier + "Intermediate",
                Attributes = MemberAttributes.Private,
                ReturnType = _nameProvider.GenerateCommandIntermediateType( featureCommand )
            };
            var parameterType = _nameProvider.GetMemberType( featureCommand.Identifier + ".Intermediate" )
                                ?? _translationProvider.ExtractType( featureCommand.IntermediateResponse[0].DataType,
                                    featureCommand.Identifier + "Intermediate" );
            conversionMethod.Parameters.Add( new CodeParameterDeclarationExpression(
                parameterType, "intermediate" ) );
            var resultRef = new CodeArgumentReferenceExpression( "intermediate" );
            var converted = GenerateClientResultConversionExpression( resultRef,
                conversionMethod.ReturnType );
            conversionMethod.Statements.Add( new CodeMethodReturnStatement( converted ) );
            return conversionMethod;
        }

        private void GenerateServerNonObservableCommand( Feature feature, FeatureCommand featureCommand, CommandSpec spec, CodeExpression implementationRef,
            CodeMemberMethod commandMethod, CodeArgumentReferenceExpression requestRef, CodeExpression serverRef )
        {
            commandMethod.ReturnType = _nameProvider.GenerateCommandResponseType( featureCommand );
            CodeStatementCollection statements = GenerateExceptionHandling( feature, featureCommand, commandMethod, serverRef );
            var actualImplementation = GenerateCommandImplementationCall( feature, featureCommand, spec, implementationRef, requestRef, new CodeFieldReferenceExpression( null, ServerFieldName ) );
            statements.Add( new CodeMethodInvokeExpression( requestRef, nameof( Argument.Validate ) ) );
            if(CodeGenerationHelper.IsSetterCommand( featureCommand, feature.Items.OfType<FeatureProperty>(), out var property ))
            {
                var valueExpression = actualImplementation.Parameters[0];
                var propertyType = _nameProvider.GetMemberType( property.Identifier );
                if(propertyType != null)
                {
                    valueExpression = new CodeCastExpression( propertyType, valueExpression );
                }
                statements.Add( new CodeAssignStatement( new CodePropertyReferenceExpression( implementationRef, featureCommand.Identifier.Substring( 3 ) ),
                    valueExpression ) );
                statements.Add( new CodeMethodReturnStatement( new CodeFieldReferenceExpression(
                    new CodeTypeReferenceExpression( nameof( EmptyRequest ) ), "Instance" ) ) );
            }
            else
            {
                if(featureCommand.Response != null && featureCommand.Response.Length > 0)
                {
                    var result = GenerateClientResultConversionExpression( actualImplementation, commandMethod.ReturnType );
                    statements.Add( new CodeMethodReturnStatement( result ) );
                }
                else
                {
                    statements.Add( new CodeExpressionStatement( actualImplementation ) );
                    statements.Add( new CodeMethodReturnStatement( new CodeFieldReferenceExpression(
                        new CodeTypeReferenceExpression( nameof( EmptyRequest ) ), "Instance" ) ) );
                }
            }
        }

        private CodeStatementCollection GenerateExceptionHandling( Feature feature, FeatureCommand featureCommand, CodeMemberMethod implementationMethod, CodeExpression serverRef )
        {
            return GenerateExceptionHandling( feature,
                featureCommand.Parameter?.ToDictionary( e => e.Identifier.ToCamelCase(), e => feature.GetFullyQualifiedParameterIdentifier( featureCommand, e.Identifier ) ),
                featureCommand.DefinedExecutionErrors, id => FindError( feature, id ), implementationMethod.Statements, serverRef );
        }

        private CodeStatementCollection GenerateExceptionHandling( Feature feature, IReadOnlyDictionary<string, string> parameters, string[] executionErrors, Func<string, FeatureDefinedExecutionError> errorFinder,
            CodeStatementCollection statements, CodeExpression serverRef )
        {
            if((executionErrors == null || executionErrors.Length == 0) && (parameters == null || parameters.Count == 0))
            {
                return statements;
            }
            else
            {
                var tryStmt = new CodeTryCatchFinallyStatement();
                statements.Add(tryStmt);
                var errorHandlingRef = new CodePropertyReferenceExpression(serverRef, nameof(ISiLAServer.ErrorHandling));
                GenerateArgumentExceptionHandlers(parameters, tryStmt, errorHandlingRef);
                if (executionErrors != null)
                {
                    foreach (var executionError in executionErrors)
                    {
                        var error = errorFinder(executionError);
                        var clause = new CodeCatchClause("ex", _nameProvider.CreateExceptionReference(executionError));
                        var exception = new CodeMethodInvokeExpression(
                            errorHandlingRef, nameof(IServerErrorHandling.CreateExecutionError),
                            new CodePrimitiveExpression(feature.GetFullyQualifiedIdentifier(error)),
                            new CodePrimitiveExpression(error.Description),
                            new CodePropertyReferenceExpression(new CodeArgumentReferenceExpression(clause.LocalName), nameof(Exception.Message)));
                        clause.Statements.Add(new CodeThrowExceptionStatement(exception));
                        tryStmt.CatchClauses.Add(clause);
                    }
                }

                return tryStmt.TryStatements;
            }
        }

        private static void GenerateArgumentExceptionHandlers(IReadOnlyDictionary<string, string> parameters, CodeTryCatchFinallyStatement tryStmt, CodePropertyReferenceExpression errorHandlingRef)
        {
            if (parameters != null && parameters.Count > 0)
            {
                var argException = new CodeCatchClause("ex", new CodeTypeReference(typeof(ArgumentException)));
                tryStmt.CatchClauses.Add(argException);
                var message = new CodePropertyReferenceExpression(new CodeArgumentReferenceExpression(argException.LocalName), nameof(Exception.Message));
                var parameterName = new CodePropertyReferenceExpression(message.TargetObject, nameof(ArgumentException.ParamName));
                foreach (var parameter in parameters)
                {
                    var exception = new CodeMethodInvokeExpression(errorHandlingRef, nameof(IServerErrorHandling.CreateValidationError),
                        new CodePrimitiveExpression(parameter.Value),
                        message);
                    argException.Statements.Add(new CodeConditionStatement(
                        new CodeBinaryOperatorExpression(parameterName, CodeBinaryOperatorType.ValueEquality, new CodePrimitiveExpression(parameter.Key)),
                        new CodeThrowExceptionStatement(exception)));
                }

                argException.Statements.Add(new CodeThrowExceptionStatement(new CodeMethodInvokeExpression(errorHandlingRef, nameof(IServerErrorHandling.CreateUnknownValidationError), message.TargetObject)));
            }
        }

        private static CodeObjectCreateExpression GenerateClientResultConversionExpression( CodeExpression expression, CodeTypeReference resultType )
        {
            var serverRef = new CodeFieldReferenceExpression( null, ServerFieldName );
            var result = new CodeObjectCreateExpression( resultType, expression, serverRef );
            return result;
        }

        private CodeMethodInvokeExpression GenerateCommandImplementationCall( Feature feature, FeatureCommand featureCommand, CommandSpec spec,
            CodeExpression implementation, CodeExpression requestRef, CodeExpression serverRef )
        {
            var actualImplementation =
                new CodeMethodInvokeExpression( implementation, _nameProvider.GetCommandName( featureCommand ) );
            if(featureCommand.Parameter != null)
            {
                foreach(var parameter in featureCommand.Parameter)
                {
                    var parameterSpec = spec?.Parameter?.FirstOrDefault( p => string.Equals( parameter.Identifier, p.Identifier ?? p.Key, StringComparison.OrdinalIgnoreCase ) );

                    var propertyName = parameter.Identifier;
                    if(parameter.Identifier == nameof( ISilaRequestObject.CommandIdentifier ))
                    {
                        propertyName += "_";
                    }
                    var valueExpression = parameterSpec?.Mapping?.ValueExpression;
                    var origin = featureCommand.Identifier + "." + parameter.Identifier;
                    var typeRef = _nameProvider?.GetMemberType( FollowIdentifier( origin, valueExpression ) );
                    var parameterValue = _translationProvider.Extract( new CodePropertyReferenceExpression( requestRef, propertyName ), parameter.DataType, serverRef, feature.GetFullyQualifiedParameterIdentifier( featureCommand, parameter.Identifier ), typeRef );
                    parameterValue = FollowExpressionReverse( parameterValue, valueExpression, origin );
                    actualImplementation.Parameters.Add( parameterValue );
                }
            }

            return actualImplementation;
        }

        private string FollowIdentifier( string identifier, Expression expression )
        {
            if(expression is PropertyExpression propertyExpression)
            {
                return identifier + "." + propertyExpression.Property;
            }
            return identifier;
        }

        private void GenerateServerRegister( Feature feature, CodeTypeDeclaration server, CodeExpression implementation )
        {
            var registerMethod = new CodeMemberMethod
            {
                Name = "Register",
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                ReturnType = new CodeTypeReference( typeof( void ) ),
            };
            var registration = new CodeArgumentReferenceExpression( "registration" );
            registerMethod.WriteDocumentation( "Registers the feature in the provided feature registration",
               parameters: new Dictionary<string, string>()
               {
                   { registration.ParameterName, "The registration component to which the feature should be registered" }
               } );
            registerMethod.Parameters.Add( new CodeParameterDeclarationExpression( nameof( IServerBuilder ), registration.ParameterName ) );

            if(feature.Items != null)
            {

                foreach(var featureCommand in feature.Items.OfType<FeatureCommand>())
                {
                    GenerateServerRegisterCommand(feature, server, registerMethod, registration, featureCommand);
                }

                foreach (var featureProperty in feature.Items.OfType<FeatureProperty>())
                {
                    GenerateServerRegisterProperty(implementation, registerMethod, registration, featureProperty);
                }

                foreach (var featureMetadata in feature.Items.OfType<FeatureMetadata>())
                {
                    registerMethod.Statements.Add( new CodeMethodInvokeExpression(
                        registration,
                        nameof( IServerBuilder.RegisterMetadata ),
                        new CodePrimitiveExpression( featureMetadata.Identifier ),
                        new CodePropertyReferenceExpression( implementation, featureMetadata.Identifier ) ) );
                }
            }
            server.Members.Add( registerMethod );
        }

        private static void GenerateServerRegisterProperty(CodeExpression implementation, CodeMemberMethod registerMethod, CodeArgumentReferenceExpression registration, FeatureProperty featureProperty)
        {
            if (featureProperty.Observable == FeaturePropertyObservable.No)
            {
                registerMethod.Statements.Add(new CodeMethodInvokeExpression(
                    registration,
                    nameof(IServerBuilder.RegisterUnobservableProperty),
                    new CodePrimitiveExpression(featureProperty.Identifier),
                    new CodeMethodReferenceExpression(null, "Get" + featureProperty.Identifier)));
            }
            else
            {
                registerMethod.Statements.Add(new CodeMethodInvokeExpression(
                    registration,
                    nameof(IServerBuilder.RegisterObservableProperty),
                    new CodePrimitiveExpression(featureProperty.Identifier),
                    new CodeMethodReferenceExpression(null, "Get" + featureProperty.Identifier),
                    implementation));
            }
        }

        private void GenerateServerRegisterCommand(Feature feature, CodeTypeDeclaration server, CodeMemberMethod registerMethod, CodeArgumentReferenceExpression registration, FeatureCommand featureCommand)
        {
            if (featureCommand.Observable == FeatureCommandObservable.No)
            {
                registerMethod.Statements.Add(new CodeMethodInvokeExpression(
                    new CodeMethodReferenceExpression(
                        registration, nameof(IServerBuilder.RegisterUnobservableCommand),
                        _nameProvider.GenerateCommandRequestType(featureCommand),
                        _nameProvider.GenerateCommandResponseType(featureCommand)),
                    new CodePrimitiveExpression(featureCommand.Identifier),
                    new CodeMethodReferenceExpression(null, featureCommand.Identifier)));
            }
            else
            {
                var typeArguments = new List<CodeTypeReference>
                        {
                            _nameProvider.GenerateCommandRequestType( featureCommand )
                        };
                var hasIntermediates = false;
                var arguments = new List<CodeExpression>
                        {
                            new CodePrimitiveExpression(featureCommand.Identifier),
                            new CodeMethodReferenceExpression( null, featureCommand.Identifier )
                        };
                if (featureCommand.IntermediateResponse != null && featureCommand.IntermediateResponse.Length > 0)
                {
                    hasIntermediates = true;
                    typeArguments.Add(_nameProvider.GetIntermediateType(featureCommand, _translationProvider));
                    typeArguments.Add(_nameProvider.GenerateCommandIntermediateType(featureCommand));
                    arguments.Add(new CodeMethodReferenceExpression(null, "Convert" + featureCommand.Identifier + "Intermediate"));
                }
                if (featureCommand.Response != null && featureCommand.Response.Length > 0)
                {
                    typeArguments.Add(_nameProvider.GetMemberType(featureCommand.Identifier + "." + featureCommand.Response[0].Identifier)
                           ?? _translationProvider.ExtractType(featureCommand.Response[0].DataType, featureCommand.Identifier));
                    typeArguments.Add(_nameProvider.GenerateCommandResponseType(featureCommand));
                    arguments.Add(new CodeMethodReferenceExpression(null, "Convert" + featureCommand.Identifier + "Response"));
                }
                if ((featureCommand.DefinedExecutionErrors != null && featureCommand.DefinedExecutionErrors.Length > 0)
                    || (featureCommand.Parameter != null && featureCommand.Parameter.Length > 0))
                {
                    arguments.Add(GenerateErrorConversionMethod(feature, featureCommand, server));
                }
                else
                {
                    arguments.Add(new CodePrimitiveExpression(null));
                }

                registerMethod.Statements.Add(new CodeMethodInvokeExpression(
                    new CodeMethodReferenceExpression(
                        registration, hasIntermediates ? nameof(IServerBuilder.RegisterCommandWithIntermediates) : nameof(IServerBuilder.RegisterObservableCommand),
                        typeArguments.ToArray()),
                    arguments.ToArray()));
            }

            GenerateServerRegisterBinaryParameters(feature, registerMethod, registration, featureCommand);
        }

        private void GenerateServerRegisterBinaryParameters(Feature feature, CodeMemberMethod registerMethod, CodeArgumentReferenceExpression registration, FeatureCommand featureCommand)
        {
            if (featureCommand.Parameter != null)
            {
                foreach (var parameter in featureCommand.Parameter)
                {
                    if (ContainsBinaries(parameter.DataType))
                    {
                        registerMethod.Statements.Add(new CodeMethodInvokeExpression(
                            registration,
                            nameof(IServerBuilder.RegisterBinaryCommandParameter),
                            new CodePrimitiveExpression(feature.GetFullyQualifiedParameterIdentifier(featureCommand, parameter.Identifier))));
                    }
                }
            }
        }

        private bool ContainsBinaries( DataTypeType dataType )
        {
            switch(dataType.Item)
            {
                case BasicType basic:
                    return basic == BasicType.Binary;
                case ListType list:
                    return ContainsBinaries( list.DataType );
                case ConstrainedType constrained:
                    return ContainsBinaries( constrained.DataType );
                case StructureType structure:
                    return structure.Element != null && structure.Element.Any( e => ContainsBinaries( e.DataType ) );
                default:
                    return false;
            }
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
            metadataInterceptor.BaseTypes.Add( typeof( IRequestInterceptor ) );
            metadataInterceptor.Members.Add( CreateAbstractProperty( nameof( IRequestInterceptor.AppliesToCommands ), typeof( bool ) )
                .WriteDocumentation( "Gets whether the metadata applies to commands", "True, if the metadata is applicable to commands, otherwise false." ) );
            metadataInterceptor.Members.Add( CreateAbstractProperty( nameof( IRequestInterceptor.AppliesToProperties ), typeof( bool ) )
                .WriteDocumentation( "Gets whether the metadata applies to properties", "True, if the metadata is applicable to properties, otherwise false." ) );
            metadataInterceptor.Members.Add( CreateAbstractProperty( nameof( IRequestInterceptor.Priority ), typeof( int ) )
                .WriteDocumentation( "Gets the priority for the metadata", "A relative priority." ) );
            metadataInterceptor.Members.Add( (new CodeMemberProperty
            {
                Name = nameof( IRequestInterceptor.MetadataIdentifier ),
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                Type = new CodeTypeReference( typeof( string ) ),
                HasGet = true,
                HasSet = false,
                GetStatements =
                {
                    new CodeMethodReturnStatement(new CodePrimitiveExpression(feature.GetFullyQualifiedIdentifier(metadata)))
                }
            }).WriteDocumentation( "Gets the metadata identifier this request interceptor is connected to or null, if not applicable" ) );
            metadataInterceptor.Members.Add( (new CodeMemberMethod
            {
                Name = nameof( IRequestInterceptor.IsInterceptRequired ),
                ReturnType = new CodeTypeReference( typeof( bool ) ),
                Attributes = MemberAttributes.Public | MemberAttributes.Abstract,
                Parameters =
                {
                    new CodeParameterDeclarationExpression( nameof(Feature), "feature" ),
                    new CodeParameterDeclarationExpression( typeof(string), "commandIdentifier" )
                }
            }).WriteDocumentation( "Decides whether the interceptor is required for the given command", "True, if the interceptor should be applied",
                new Dictionary<string, string>()
                {
                    { "feature", "The feature that contains the command or property" },
                    { "commandIdentifier", "The fully qualified identifier of the command or property in question" }
                } ) );
            var metadataType = _translationProvider.ExtractType( metadata.DataType, metadata.Identifier );
            metadataInterceptor.Members.Add( (new CodeMemberMethod
            {
                Name = nameof( IRequestInterceptor.Intercept ),
                ReturnType = new CodeTypeReference( typeof( IRequestInterception ) ),
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
                Name = nameof( IRequestInterceptor.Intercept ),
                ReturnType = new CodeTypeReference( typeof( IRequestInterception ) ),
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                Parameters =
                {
                    new CodeParameterDeclarationExpression( typeof(string), "commandIdentifier" ),
                    new CodeParameterDeclarationExpression( typeof(ISiLAServer), "server" ),
                    new CodeParameterDeclarationExpression( typeof(IMetadataRepository), "metadata" )
                }
            };
            var serverRef = new CodeArgumentReferenceExpression( "server" );
            var extractMetadata = new CodeMethodInvokeExpression(
                new CodeMethodReferenceExpression( new CodeTypeReferenceExpression( typeof( Metadata ) ), nameof( Metadata.Extract ),
                    _translationProvider.GetDtoTypeReference( metadata.DataType, metadata.Identifier, structHandler ) ),
                new CodeArgumentReferenceExpression( "metadata" ),
                new CodePropertyReferenceExpression( null, nameof( IRequestInterceptor.MetadataIdentifier ) ) );
            var statements = GenerateExceptionHandling( feature, null, metadata.DefinedExecutionErrors, id => FindError( feature, id ), interceptMethod.Statements, serverRef );
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
                    { "metadata", "The metadata attached to the request" }
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

#pragma warning restore S3265 // Non-flags enums should not be used in bitwise operations