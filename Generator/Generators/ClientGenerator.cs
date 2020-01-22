using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Grpc.Core;
using Tecan.Sila2.Client;
using Tecan.Sila2.Generator.Contracts;
using Tecan.Sila2.Generator.Helper;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.Generator.Generators
{
    /// <summary>
    /// Generates client classes that implement a feature interface using SiLA
    /// </summary>
    [Export(typeof(IClientGenerator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ClientGenerator : IClientGenerator
    {
        private const string ExecutionManagerFieldName = "_executionManager";

        private readonly ICodeNameProvider _nameProvider;
        private readonly IEnumerable<IGeneratorHook> _hooks;
        private readonly ITypeTranslationProvider _translationProvider;

        /// <summary>
        /// Creates a new client generator
        /// </summary>
        /// <param name="nameProvider">The name provider used to generate feature names</param>
        /// <param name="translationProvider">A component used to translate types between SiLA2 and .NET</param>
        /// <param name="hooks">Generator hooks</param>
        [ImportingConstructor]
        public ClientGenerator(ICodeNameProvider nameProvider, ITypeTranslationProvider translationProvider, [ImportMany] IEnumerable<IGeneratorHook> hooks)
        {
            _nameProvider = nameProvider;
            _hooks = hooks;
            _translationProvider = translationProvider;
        }


        /// <inheritdoc />
        public CodeTypeDeclaration GenerateClient(Feature feature)
        {
            var client = new CodeTypeDeclaration("Client")
            {
                Attributes = MemberAttributes.Public,
                TypeAttributes = TypeAttributes.Public,
                IsClass = true,
                IsPartial = true
            };
            client.BaseTypes.Add(new CodeTypeReference(typeof(ClientBase<>).FullName,
                new CodeTypeReference(client.Name)));
            client.BaseTypes.Add(_nameProvider.CreateFeatureInterfaceReference(feature));
            GenerateClientBaseImplementation(feature, client);
            foreach (var featureCommand in feature.Items.OfType<FeatureCommand>())
            {
                GenerateClientCommand(feature, featureCommand, client);
            }

            GenerateClientNewInstanceMethod(client);
            GenerateClientExtractMethod(client);
            if (_hooks != null)
            {
                foreach (var hook in _hooks)
                {
                    hook?.OnClientGenerated(feature, client);
                }
            }

            client.WriteDocumentation($"Class that implements the {_nameProvider.CreateFeatureInterfaceReference(feature).BaseType} interface through SiLA2");
            return client;
        }

        private void GenerateClientBaseImplementation(Feature feature, CodeTypeDeclaration client)
        {
            var initLazyRequests = new CodeMemberMethod()
            {
                Name = "InitLazyRequests",
                Attributes = MemberAttributes.Private | MemberAttributes.Final
            };
            initLazyRequests.WriteDocumentation("Initializes lazies for non-observable properties.");
            var anyLazy = false;
            var anyDynamic = false;
            foreach (var featureProperty in feature.Items.OfType<FeatureProperty>())
            {
                anyLazy |= featureProperty.Observable == FeaturePropertyObservable.No;
                anyDynamic |= featureProperty.Observable == FeaturePropertyObservable.Yes;
                GenerateClientProperty(feature, featureProperty, client, initLazyRequests);
            }

            if (anyDynamic)
            {
                client.BaseTypes.Add(typeof(INotifyPropertyChanged));
                client.Members.Add(new CodeMemberEvent()
                {
                    Name = nameof(INotifyPropertyChanged.PropertyChanged),
                    Attributes = MemberAttributes.Public,
                    Type = new CodeTypeReference(typeof(PropertyChangedEventHandler))
                });
            }

            if (anyLazy) client.Members.Add(initLazyRequests);
            GenerateClientConstructors(client, anyLazy);
        }

        private void GenerateClientNewInstanceMethod(CodeTypeDeclaration client)
        {
            var selfType = new CodeTypeReference(client.Name);
            var newInstance = new CodeMemberMethod()
            {
                Name = "NewInstance",
                Attributes = MemberAttributes.Family | MemberAttributes.Override,
                ReturnType = selfType
            };
            newInstance.WriteDocumentation("Creates a new instance", parameters: new Dictionary<string, string>()
            {
                { "configuration", "The new client configuration" }
            });
            newInstance.Parameters.Add(
                new CodeParameterDeclarationExpression("ClientBaseConfiguration", "configuration"));
            newInstance.Statements.Add(new CodeMethodReturnStatement(
                new CodeObjectCreateExpression(
                    selfType,
                    new CodeArgumentReferenceExpression("configuration"),
                    new CodeFieldReferenceExpression(null, ExecutionManagerFieldName)
                )));
            client.Members.Add(newInstance);
        }

        private void GenerateClientExtractMethod( CodeTypeDeclaration client )
        {
            var T = new CodeTypeReference( "T" );
            var extract = new CodeMemberMethod()
            {
                Name = nameof(ISilaTransferObject<object>.Extract),
                Attributes = MemberAttributes.Private,
                ReturnType = T
            };
            extract.TypeParameters.Add( new CodeTypeParameter( "T" ) );
            extract.Parameters.Add(
                new CodeParameterDeclarationExpression( new CodeTypeReference( typeof(ISilaTransferObject<>).FullName, T ),
                    "dto" ) );
            extract.Statements.Add( new CodeMethodReturnStatement(
                new CodeMethodInvokeExpression( new CodeArgumentReferenceExpression( "dto" ), extract.Name, new CodePropertyReferenceExpression(
                    new CodeFieldReferenceExpression( null, ExecutionManagerFieldName ),
                    nameof(IClientExecutionManager.DownloadBinaryStore) ) ) ) );
            client.Members.Add( extract );
        }

        private void GenerateClientCommand(Feature feature, FeatureCommand featureCommand, CodeTypeDeclaration client)
        {
            var requestMethod = new CodeMemberMethod()
            {
                Name = _nameProvider.GetCommandName(featureCommand),
                Attributes = MemberAttributes.Public
            };
            var parameterDictionary = new Dictionary<string, string>();
            client.Members.Add(requestMethod);
            var requestType = _nameProvider.GenerateCommandRequestType(featureCommand);
            var request = new CodeObjectCreateExpression(requestType);
            var requestRef = new CodeVariableReferenceExpression(nameof(request));
            string binaryParameterIdentifier = null;
            if( featureCommand.Parameter != null )
            {
                foreach( var parameterType in featureCommand.Parameter )
                {
                    var parameterName = parameterType.Identifier.ToCamelCase();
                    requestMethod.Parameters.Add( new CodeParameterDeclarationExpression(
                        _nameProvider?.GetMemberType( featureCommand.Identifier + "." + parameterType.Identifier )
                        ?? _translationProvider.ExtractType( parameterType.DataType, featureCommand.Identifier + parameterType.Identifier ), parameterName ) );
                    request.Parameters.Add( new CodeArgumentReferenceExpression( parameterName ) );
                    parameterDictionary.Add( parameterName, parameterType.Description );
                }

                var binaryParameter = featureCommand.Parameter.SingleOrDefault( p => ContainsBinaries( p.DataType, id => feature.Items.OfType<SiLAElement>().Single(e => e.Identifier == id).DataType ) );
                if( binaryParameter != null )
                {
                    binaryParameterIdentifier = feature.GetFullyQualifiedParameterIdentifier( featureCommand, binaryParameter.Identifier );
                }
            }

            requestMethod.WriteDocumentation(featureCommand.Description, parameters: parameterDictionary);
            
            request.Parameters.Add(GenerateCreateBinaryStore(binaryParameterIdentifier));
            requestMethod.Statements.Add(new CodeVariableDeclarationStatement(requestType, nameof(request), request));


            if (featureCommand.Observable == FeatureCommandObservable.No)
            {
                GenerateClientNonObservableCommandImplementation(feature, featureCommand, client, requestMethod, requestRef);
            }
            else
            {
                GenerateClientObservableCommandImplementation(feature, featureCommand, client, requestMethod, requestRef);
            }
        }

        private bool ContainsBinaries( DataTypeType dataType, Func<string, DataTypeType> typeResolver )
        {
            switch( dataType.Item )
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

        private static CodeExpression GenerateCreateBinaryStore(string commandParameterIdentifier)
        {
            if( commandParameterIdentifier == null )
            {
                return new CodePrimitiveExpression();
            }
            return new CodeMethodInvokeExpression(new CodeFieldReferenceExpression(null, ExecutionManagerFieldName), nameof(IClientExecutionManager.CreateBinaryStore), new CodePrimitiveExpression(commandParameterIdentifier));
        }

        private void GenerateClientObservableCommandImplementation(Feature feature, FeatureCommand featureCommand,
            CodeTypeDeclaration client,
            CodeMemberMethod requestMethod,
            CodeVariableReferenceExpression requestRef)
        {
            var exceptionConverter =
                GenerateClientConvertExceptionMethod(feature, featureCommand.Identifier, featureCommand.DefinedExecutionErrors);
            client.Members.Add(exceptionConverter);
            CodeTypeReference intermediateType = GetIntermediateType(featureCommand);
            var callInfo = new CodeMethodInvokeExpression(new CodeFieldReferenceExpression(null, ExecutionManagerFieldName),
                nameof(IClientExecutionManager.CreateCallOptions),
                new CodePropertyReferenceExpression(requestRef, nameof(ISilaRequestObject.CommandIdentifier)));
            var actualCall = new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression(nameof(ObservableCommand)),
                intermediateType != null ? nameof(ObservableCommand.CreateIntermediate) : nameof(ObservableCommand.Create),
                callInfo,
                new CodePropertyReferenceExpression(null, "CallInvoker"),
                requestRef,
                new CodeFieldReferenceExpression(null, featureCommand.Identifier + "Method"),
                new CodeFieldReferenceExpression(null, featureCommand.Identifier + "StateMethod"));
            
            if (intermediateType != null)
            {
                actualCall.Parameters.Add(new CodeFieldReferenceExpression(null, featureCommand.Identifier + "IntermediateMethod"));
            }
            if (featureCommand.Response != null && featureCommand.Response.Length > 0)
            {
                var responseType = _nameProvider?.GetMemberType(featureCommand.Identifier + "." + featureCommand.Response[0].Identifier)
                    ?? _translationProvider.ExtractType(featureCommand.Response[0].DataType, featureCommand.Identifier );

                var convertResponseMethod = GenerateClientCommandResponseMethod(
                    featureCommand.Identifier + "Response",
                    featureCommand.Response, responseType,
                    client);
                actualCall.Parameters.Add(
                    new CodeMethodReferenceExpression(null, featureCommand.Identifier + "ResponseMethod" ) );
                if (intermediateType != null)
                {
                    requestMethod.ReturnType = new CodeTypeReference(typeof(IIntermediateObservableCommand<,>).FullName, intermediateType, responseType);
                    var convertIntermediateResponseMethod = GenerateClientCommandResponseMethod(
                        featureCommand.Identifier + "Intermediate",
                        featureCommand.IntermediateResponse,
                        intermediateType,
                        client);
                    actualCall.Parameters.Add(new CodeMethodReferenceExpression(null, convertIntermediateResponseMethod.Name));
                }
                else
                {
                    requestMethod.ReturnType = new CodeTypeReference(typeof(IObservableCommand<>).FullName, responseType);
                }
                actualCall.Parameters.Add(new CodeMethodReferenceExpression(null, convertResponseMethod.Name));
            }
            else
            {
                if (intermediateType != null)
                {
                    requestMethod.ReturnType = new CodeTypeReference(typeof(IIntermediateObservableCommand<>).FullName, intermediateType);
                    var convertIntermediateResponseMethod = GenerateClientCommandResponseMethod(
                        featureCommand.Identifier + "Intermediate",
                        featureCommand.IntermediateResponse, intermediateType,
                        client);
                    actualCall.Parameters.Add(new CodeMethodReferenceExpression(null, convertIntermediateResponseMethod.Name));
                }
                else
                {
                    requestMethod.ReturnType = new CodeTypeReference(typeof(IObservableCommand));
                }
            }

            actualCall.Parameters.Add(new CodeMethodReferenceExpression(null, exceptionConverter.Name));

            requestMethod.Statements.Add(new CodeMethodReturnStatement(actualCall));
        }

        private CodeTypeReference GetIntermediateType(FeatureCommand featureCommand)
        {
            if (featureCommand.IntermediateResponse != null && featureCommand.IntermediateResponse.Length > 0)
            {
                return _nameProvider?.GetMemberType(featureCommand.Identifier + "." + featureCommand.IntermediateResponse[0].Identifier)
                                ?? _translationProvider.ExtractType( featureCommand.IntermediateResponse[0].DataType, featureCommand.Identifier + "Intermediate" );
            }
            return null;
        }

        private CodeMemberMethod GenerateClientCommandResponseMethod(string name, SiLAElement[] elements, CodeTypeReference returnType,
            CodeTypeDeclaration client)
        {
            var convertResponseMethod = new CodeMemberMethod
            {
                Name = "Convert" + name,
                Attributes = MemberAttributes.Private,
                ReturnType = returnType
            };
            var valueParameter =
                new CodeParameterDeclarationExpression(name + "Dto", "value");
            convertResponseMethod.Parameters.Add(valueParameter);
            convertResponseMethod.WriteDocumentation($"Unwraps the response of the {name} command", "The actual response", new Dictionary<string, string>()
            {
                {valueParameter.Name, "The response data transfer object" }
            });
            convertResponseMethod.Statements.Add(new CodeMethodReturnStatement(GenerateClientConvertResponse(
                elements[0].DataType,
                elements[0].Identifier,
                new CodeArgumentReferenceExpression(valueParameter.Name),
                _nameProvider.GetMemberType(name + ".Return"))));
            client.Members.Add(convertResponseMethod);
            return convertResponseMethod;
        }

        private void GenerateClientNonObservableCommandImplementation(Feature feature, FeatureCommand featureCommand,
            CodeTypeDeclaration client,
            CodeMemberMethod requestMethod, CodeVariableReferenceExpression requestRef)
        {
            var identifier = feature.GetFullyQualifiedIdentifier(featureCommand);
            var callInfo = new CodeMethodInvokeExpression(
                new CodeFieldReferenceExpression(null, ExecutionManagerFieldName),
                nameof(IClientExecutionManager.CreateCallOptions),
                new CodePrimitiveExpression(identifier));
            var callInfoRef = new CodeVariableReferenceExpression(nameof(callInfo));
            requestMethod.Statements.Add(new CodeVariableDeclarationStatement(typeof(IClientCallInfo), callInfoRef.VariableName, callInfo));

            CodeExpression ret;
            if (featureCommand.Response != null && featureCommand.Response.Length > 0)
            {
                if (featureCommand.Response.Length == 1)
                {
                    var customType = _nameProvider.GetMemberType( featureCommand.Identifier + "." + featureCommand.Response[0].Identifier );
                    requestMethod.ReturnType = customType ?? _translationProvider.ExtractType( featureCommand.Response[0].DataType, featureCommand.Identifier );
                    var response = GenerateClientGrpcInvoke(featureCommand.Identifier + "Method",
                        requestRef, featureCommand.Response[0].DataType,
                        featureCommand.Response[0].Identifier,
                        callInfoRef, customType);
                    ret = response;
                }
                else
                {
                    requestMethod.ReturnType = new CodeTypeReference(featureCommand.Identifier + "Response");
                    var response = GenerateClientGrpcInvoke(featureCommand.Identifier + "Method",
                        requestRef, null, null, callInfoRef, null);
                    ret = new CodeMethodInvokeExpression(response,
                        nameof(ISilaTransferObject<object>.Extract),
                        new CodePropertyReferenceExpression(
                            new CodeFieldReferenceExpression(null, ExecutionManagerFieldName),
                            nameof(IClientExecutionManager.DownloadBinaryStore)));
                }
            }
            else
            {
                ret = GenerateClientGrpcInvoke(featureCommand.Identifier + "Method",
                    requestRef, null, null, callInfoRef, null);
            }

            requestMethod.Statements.Add(GenerateClientExceptionHandling(feature, client, ret, featureCommand.Response != null && featureCommand.Response.Length > 0 ? requestMethod.ReturnType : null,
                    featureCommand.DefinedExecutionErrors, featureCommand.Identifier, callInfoRef));
        }

        private CodeTryCatchFinallyStatement GenerateClientExceptionHandling(Feature feature, CodeTypeDeclaration client, CodeExpression response, CodeTypeReference responseType, string[] errors,
            string name, CodeExpression callInfo)
        {
            var tryStmt = new CodeTryCatchFinallyStatement();
            var catchAll = new CodeCatchClause("ex", new CodeTypeReference(typeof(Exception)));
            tryStmt.CatchClauses.Add(catchAll);
            var errorHandler =
                new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(nameof(ErrorHandling)),
                    nameof(ErrorHandling.ConvertException));
            var exceptionRef = new CodeVariableReferenceExpression("exception");
            catchAll.Statements.Add(new CodeVariableDeclarationStatement(typeof(Exception), exceptionRef.VariableName, errorHandler));
            catchAll.Statements.Add(new CodeMethodInvokeExpression(callInfo, nameof(IClientCallInfo.FinishWithErrors), exceptionRef));
            catchAll.Statements.Add(new CodeThrowExceptionStatement(exceptionRef));
            errorHandler.Parameters.Add(new CodeArgumentReferenceExpression(catchAll.LocalName));
            if (errors != null && errors.Length > 0)
            {
                var convertExceptionMethod = GenerateClientConvertExceptionMethod(feature, name, errors);
                client.Members.Add(convertExceptionMethod);
                errorHandler.Parameters.Add(new CodeMethodReferenceExpression(null, convertExceptionMethod.Name));
            }
            if (responseType != null)
            {
                var responseRef = new CodeVariableReferenceExpression(nameof(response));
                tryStmt.TryStatements.Add(new CodeVariableDeclarationStatement(responseType, responseRef.VariableName, response));
                tryStmt.TryStatements.Add(new CodeMethodInvokeExpression(callInfo, nameof(IClientCallInfo.FinishSuccessful)));
                tryStmt.TryStatements.Add(new CodeMethodReturnStatement(responseRef));
            }
            else
            {
                tryStmt.TryStatements.Add(response);
                tryStmt.TryStatements.Add(new CodeMethodInvokeExpression(callInfo, nameof(IClientCallInfo.FinishSuccessful)));
                tryStmt.TryStatements.Add(new CodeMethodReturnStatement());
            }
            return tryStmt;
        }

        private CodeMemberMethod GenerateClientConvertExceptionMethod(Feature feature, string name, string[] errors)
        {
            var convertExceptionMethod = new CodeMemberMethod
            {
                Name = "Convert" + name + "Exception",
                Attributes = MemberAttributes.Private | MemberAttributes.Static,
                ReturnType = new CodeTypeReference(typeof(Exception))
            };
            convertExceptionMethod.Parameters.Add(
                new CodeParameterDeclarationExpression(typeof(DefinedExecutionErrorDto), "error"));
            convertExceptionMethod.WriteDocumentation($"Converts the error ocurred during execution of {name} to a proper exception", "The converted exception or null, if the error is not understood",
                new Dictionary<string, string>()
                {
                    {"error", "The SiLA2 execution error" }
                });
            var errorRef = new CodeArgumentReferenceExpression("error");
            var identityRef = new CodePropertyReferenceExpression(errorRef, nameof(DefinedExecutionErrorDto.ErrorIdentifier));
            var messageRef = new CodePropertyReferenceExpression(errorRef, nameof(DefinedExecutionErrorDto.Message));
            if (errors != null)
            {
                foreach (var error in errors)
                {
                    var definedError = feature.Items.OfType<FeatureDefinedExecutionError>().FirstOrDefault( e => e.Identifier == error );
                    var errorIdentifier = feature.GetFullyQualifiedIdentifier( definedError );
                    var ifError = new CodeConditionStatement
                    {
                        Condition = new CodeBinaryOperatorExpression(
                            identityRef,
                            CodeBinaryOperatorType.ValueEquality,
                            new CodePrimitiveExpression(errorIdentifier))
                    };
                    var exception =
                        new CodeObjectCreateExpression(_nameProvider.CreateExceptionReference(error), messageRef);
                    ifError.TrueStatements.Add(new CodeMethodReturnStatement(exception));
                    convertExceptionMethod.Statements.Add(ifError);
                }
            }

            convertExceptionMethod.Statements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression()));
            return convertExceptionMethod;
        }

        private CodeExpression GenerateClientGrpcInvoke(string methodName, CodeExpression request,
            DataTypeType responseType, string propertyName, CodeExpression callInfoRef, CodeTypeReference customResponseType)
        {
            CodeExpression response = new CodeMethodInvokeExpression(
                new CodePropertyReferenceExpression(null, "CallInvoker"),
                "BlockingUnaryCall",
                new CodeFieldReferenceExpression(null, methodName),
                new CodePrimitiveExpression(null),
                new CodePropertyReferenceExpression(callInfoRef, "Options"),
                request);
            return GenerateClientConvertResponse(responseType, propertyName, response, customResponseType);
        }

        private CodeExpression GenerateClientConvertResponse(DataTypeType responseType, string propertyName,
            CodeExpression response, CodeTypeReference customType)
        {
            if (responseType != null)
            {
                response = new CodePropertyReferenceExpression(response, propertyName);
                return _translationProvider.Extract( response, responseType,
                    new CodePropertyReferenceExpression( new CodeFieldReferenceExpression( null, ExecutionManagerFieldName ), nameof(IClientExecutionManager.DownloadBinaryStore) ),
                    customType );
            }

            return response;
        }

        private void GenerateClientProperty(Feature feature, FeatureProperty featureProperty, CodeTypeDeclaration client,
            CodeMemberMethod initLazyRequests)
        {
            var propertyType = _nameProvider?.GetMemberType(featureProperty.Identifier)
                               ?? _translationProvider.ExtractType( featureProperty.DataType, featureProperty.Identifier );
            var property = new CodeMemberProperty()
            {
                Name = _nameProvider.GetPropertyName(featureProperty),
                Attributes = MemberAttributes.Public,
                Type = propertyType,
                HasSet = false,
                HasGet = true
            };
            property.WriteDocumentation(featureProperty.Description ?? $"The {featureProperty.DisplayName} property");
            client.Members.Add(property);
            if (featureProperty.Observable == FeaturePropertyObservable.No)
            {
                GenerateClientStaticProperty(feature, featureProperty, client, initLazyRequests, propertyType, property);
            }
            else
            {
                GenerateClientDynamicProperty(feature, featureProperty, client, propertyType, property);
            }
        }

        private void GenerateClientStaticProperty(Feature feature, FeatureProperty featureProperty, CodeTypeDeclaration client,
            CodeMemberMethod initLazyRequests, CodeTypeReference propertyType, CodeMemberProperty property)
        {
            var fieldName = "_" + featureProperty.Identifier.ToCamelCase();
            var fieldRef = new CodeFieldReferenceExpression(null, fieldName);
            var lazyType = new CodeTypeReference(typeof(Lazy<>).FullName, propertyType);
            client.Members.Add(new CodeMemberField(lazyType, fieldName));
            var requestMethod = new CodeMemberMethod()
            {
                Name = "Request" + property.Name,
                Attributes = MemberAttributes.Private,
                ReturnType = propertyType
            };
            client.Members.Add(requestMethod);
            initLazyRequests.Statements.Add(new CodeAssignStatement(
                fieldRef,
                new CodeObjectCreateExpression(lazyType,
                    new CodeMethodReferenceExpression(null, requestMethod.Name))));
            property.GetStatements.Add(
                new CodeMethodReturnStatement(new CodePropertyReferenceExpression(fieldRef, "Value")));
            var identifier = feature.GetFullyQualifiedIdentifier(featureProperty);
            var callInfo = new CodeMethodInvokeExpression(
                new CodeFieldReferenceExpression(null, ExecutionManagerFieldName),
                nameof(IClientExecutionManager.CreateCallOptions),
                new CodePrimitiveExpression(identifier));
            var callInfoRef = new CodeVariableReferenceExpression(nameof(callInfo));
            requestMethod.Statements.Add(new CodeVariableDeclarationStatement(typeof(IClientCallInfo), callInfoRef.VariableName, callInfo));

            CodeExpression response = GenerateClientGrpcInvoke("Get" + featureProperty.Identifier + "Method",
                new CodePropertyReferenceExpression(new CodeTypeReferenceExpression(nameof(EmptyRequest)),
                    "Instance"), featureProperty.DataType, "Value", callInfoRef, _nameProvider.GetMemberType(featureProperty.Identifier));
            requestMethod.Statements.Add(GenerateClientExceptionHandling(feature, client, response, propertyType,
                featureProperty.DefinedExecutionErrors, featureProperty.Identifier, callInfoRef));
        }

        private void GenerateClientDynamicProperty(Feature feature, FeatureProperty featureProperty, CodeTypeDeclaration client,
            CodeTypeReference propertyType, CodeMemberProperty property)
        {
            var fieldName = "_" + featureProperty.Identifier.ToCamelCase();
            var fieldRef = new CodeFieldReferenceExpression(null, fieldName);
            client.Members.Add(new CodeMemberField(propertyType, fieldName));
            var updateTask = new CodeFieldReferenceExpression(null, fieldName + "UpdateTask");
            client.Members.Add(new CodeMemberField(typeof(Task), updateTask.FieldName));

            var receiveValueMethod = GenerateClientReceiveValueMethod(featureProperty, fieldName, fieldRef, property);
            var subscription = new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression(nameof(ObservableCommands)),
                nameof(ObservableCommands.SubscribeTo),
                new CodePropertyReferenceExpression(null, "CallInvoker"),
                new CodeMethodReferenceExpression(null, "Subscribe" + featureProperty.Identifier + "Method"),
                new CodeMethodReferenceExpression(null, receiveValueMethod.Name),
                new CodeMethodInvokeExpression(new CodeFieldReferenceExpression(null, ExecutionManagerFieldName), nameof(IClientExecutionManager.CreateCallOptions), new CodePrimitiveExpression(feature.GetFullyQualifiedIdentifier(featureProperty))));
            var ifUpdateTaskNull = new CodeConditionStatement(
                new CodeBinaryOperatorExpression(updateTask, CodeBinaryOperatorType.IdentityEquality,
                    new CodePrimitiveExpression()),
                new CodeAssignStatement(updateTask, subscription));
            property.GetStatements.Add(ifUpdateTaskNull);
            property.GetStatements.Add(new CodeMethodReturnStatement(fieldRef));
            client.Members.Add(receiveValueMethod);
        }

        private CodeMemberMethod GenerateClientReceiveValueMethod(FeatureProperty featureProperty, string fieldName,
            CodeFieldReferenceExpression fieldRef, CodeMemberProperty property)
        {
            var receiveValueMethod = new CodeMemberMethod
            {
                Name = "ReceiveNew" + featureProperty.Identifier,
                Attributes = MemberAttributes.Private
            };
            var parameter = new CodeParameterDeclarationExpression(
                CodeGenerationHelper.GetPropertyResponseType( _translationProvider, featureProperty),
                fieldName.Substring(1));
            receiveValueMethod.Parameters.Add(parameter);
            var inner = new CodeVariableDeclarationStatement(
                _translationProvider.ExtractType( featureProperty.DataType, featureProperty.Identifier ),
                "inner",
                new CodeMethodInvokeExpression(
                    new CodePropertyReferenceExpression(new CodeArgumentReferenceExpression(parameter.Name), "Value"),
                    nameof(ISilaTransferObject<object>.Extract),
                    new CodePropertyReferenceExpression(
                        new CodeFieldReferenceExpression( null, ExecutionManagerFieldName ),
                        nameof( IClientExecutionManager.DownloadBinaryStore ) ) ) );
            receiveValueMethod.Statements.Add(inner);
            var innerRef = new CodeVariableReferenceExpression(inner.Name);
            var ifChanged = new CodeConditionStatement(
                new CodeBinaryOperatorExpression(fieldRef, CodeBinaryOperatorType.IdentityInequality, innerRef));
            ifChanged.TrueStatements.Add(new CodeAssignStatement(fieldRef, innerRef));
            GenerateRaisePropertyChanged(ifChanged.TrueStatements, property.Name);
            receiveValueMethod.Statements.Add(ifChanged);
            return receiveValueMethod;
        }

        private static void GenerateRaisePropertyChanged(CodeStatementCollection statements, string propertyName)
        {
            var handler = new CodeVariableDeclarationStatement(typeof(PropertyChangedEventHandler), "handler",
                new CodeFieldReferenceExpression(null, nameof(INotifyPropertyChanged.PropertyChanged)));
            statements.Add(handler);
            var handlerRef = new CodeVariableReferenceExpression(handler.Name);
            var eventArgs =
                new CodeObjectCreateExpression(typeof(PropertyChangedEventArgs),
                    new CodePrimitiveExpression(propertyName));
            statements.Add(new CodeConditionStatement(
                new CodeBinaryOperatorExpression(handlerRef, CodeBinaryOperatorType.IdentityInequality,
                    new CodePrimitiveExpression()),
                new CodeExpressionStatement(new CodeMethodInvokeExpression(handlerRef, "Invoke",
                    new CodeThisReferenceExpression(), eventArgs))));
        }

        private void GenerateClientConstructors(CodeTypeDeclaration client, bool initLazyRequests)
        {
            var callInitLazyRequests = new CodeMethodInvokeExpression(null, "InitLazyRequests");
            var defaultConstructor = new CodeConstructor() { Attributes = MemberAttributes.Public };
            var configurationConstructor = new CodeConstructor() { Attributes = MemberAttributes.Family };
            var channelConstructor = new CodeConstructor() { Attributes = MemberAttributes.Public };
            configurationConstructor.Parameters.Add(
                new CodeParameterDeclarationExpression("ClientBaseConfiguration", "configuration"));
            configurationConstructor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("configuration"));
            channelConstructor.Parameters.Add(new CodeParameterDeclarationExpression(typeof(Channel), "channel"));
            channelConstructor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("channel"));
            var resolverParameter = new CodeParameterDeclarationExpression(typeof(IClientExecutionManager).Name, "executionManager");
            var resolverField = new CodeMemberField(typeof(IClientExecutionManager).Name, ExecutionManagerFieldName);
            client.Members.Add(resolverField);
            var resolverAssignment = new CodeAssignStatement(new CodeFieldReferenceExpression(null, resolverField.Name), new CodeArgumentReferenceExpression(resolverParameter.Name));

            FinishConstructor(defaultConstructor, client, initLazyRequests, callInitLazyRequests, resolverParameter, resolverAssignment);
            FinishConstructor(configurationConstructor, client, initLazyRequests, callInitLazyRequests, resolverParameter, resolverAssignment);
            FinishConstructor(channelConstructor, client, initLazyRequests, callInitLazyRequests, resolverParameter, resolverAssignment);
        }

        private static void FinishConstructor(CodeConstructor constructor, CodeTypeDeclaration client, bool initLazyRequests, CodeMethodInvokeExpression callInitLazyRequests, CodeParameterDeclarationExpression resolverParameter, CodeAssignStatement resolverAssignment)
        {
            constructor.Parameters.Add(resolverParameter);
            if (initLazyRequests)
            {
                constructor.Statements.Add(callInitLazyRequests);
            }
            constructor.Statements.Add(resolverAssignment);
            client.Members.Add(constructor);
        }
    }
}
