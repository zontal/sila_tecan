using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Common.Logging;
using Microsoft.SqlServer.Server;
using ProtoBuf;
using Tecan.Sila2.Generator.Contracts;
using Tecan.Sila2.Generator.Helper;

#pragma warning disable S3265 // Non-flags enums should not be used in bitwise operations

namespace Tecan.Sila2.Generator.Generators
{
    /// <summary>
    /// Generates data transfer objects
    /// </summary>
    [Export( typeof( IDtoGenerator ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class DtoGenerator : IDtoGenerator
    {
        private const string BinaryStoreParameterName = "store";

        /// <summary>
        /// The name provider for the Dto generator
        /// </summary>
        public ICodeNameProvider NameProvider { get; }

        private readonly ITypeTranslationProvider _translationProvider;
        private readonly ILog _loggingChannel = LogManager.GetLogger<DtoGenerator>();
        private readonly IGeneratorConfigSource _configSource;
        private readonly IReadOnlyList<IValidationCreator> _validators;

        /// <summary>
        /// Creates a new Dto generator for the given name provider
        /// </summary>
        /// <param name="translationProvider">The translation provider</param>
        /// <param name="nameProvider">A name provider</param>
        /// <param name="configSource">A component where to draw configuration from</param>
        /// <param name="validations">Validators to generate validation statements</param>
        [ImportingConstructor]
        public DtoGenerator( ITypeTranslationProvider translationProvider, ICodeNameProvider nameProvider, IGeneratorConfigSource configSource, [ImportMany] IEnumerable<IValidationCreator> validations )
        {
            _translationProvider = translationProvider;
            _configSource = configSource;
            _validators = validations.OrderBy( v => v.GetType().Name, StringComparer.InvariantCulture ).ToList();
            NameProvider = nameProvider;
        }

        /// <inheritdoc />
        public CodeCompileUnit GenerateInterfaceUnit( Feature feature, string ns )
        {
            _loggingChannel.Info($"Generating data transfer objects for feature {feature.FullyQualifiedIdentifier}");
            var unit = new CodeCompileUnit();
            unit.Namespaces.Add(new CodeNamespace
            {
                Imports =
                {
                    new CodeNamespaceImport("System"),
                    new CodeNamespaceImport("Tecan.Sila2")
                }
            });
            var nSpace = new CodeNamespace(ns);
            unit.Namespaces.Add(nSpace);

            var structureHelper = new AnonymousTypeHelper();
            var structHandler = new Action<string, StructureType>(structureHelper.RegisterAnonymousType);

            if (feature.Items != null)
            {
                var spec = _configSource.GetFeatureSpec(feature.Identifier);
                foreach (var featureCommand in feature.Items.OfType<FeatureCommand>())
                {
                    GenerateCommand(feature, nSpace, structHandler, spec, featureCommand);
                }

                foreach (var featureProperty in feature.Items.OfType<FeatureProperty>())
                {
                    GenerateProperty(nSpace, structHandler, spec, featureProperty);
                }

                foreach (var dataTypeDefinition in feature.Items.OfType<SiLAElement>())
                {
                    GenerateDataType(nSpace, structHandler, spec, dataTypeDefinition);
                }

                foreach (var metadata in feature.Items.OfType<FeatureMetadata>())
                {
                    _translationProvider.GetDtoTypeReference(metadata.DataType, metadata.Identifier, structHandler);
                }
            }
            else
            {
                _loggingChannel.Warn($"The feature {feature.Identifier} has no items.");
            }

            AddRemainingDataTypes(nSpace, structureHelper, structHandler);

            return unit;
        }

        private void AddRemainingDataTypes(CodeNamespace nSpace, AnonymousTypeHelper structureHelper, Action<string, StructureType> structHandler)
        {
            structureHelper.ProcessAll((name, anonymousType) =>
            {
                _loggingChannel.Debug($"Generating data transfer type for anonymous type {name}");
                var element = new SiLAElement()
                {
                    Identifier = name,
                    DataType = new DataTypeType()
                    {
                        Item = anonymousType
                    },
                    DisplayName = name.ToDisplayName()
                };
                var type = GenerateDataTypeDefinition(element, null, structHandler, false);
                if (type != null)
                {
                    nSpace.Types.Add(type);
                }
            });
        }

        private void GenerateDataType(CodeNamespace nSpace, Action<string, StructureType> structHandler, FeatureSpec spec, SiLAElement dataTypeDefinition)
        {
            _loggingChannel.Debug($"Generating data transfer type for {dataTypeDefinition.Identifier}");
            var typeSpec = spec?.Type?.FirstOrDefault(t => dataTypeDefinition.Identifier == (t.Identifier ?? t.Code));
            var type = GenerateDataTypeDefinition(dataTypeDefinition, typeSpec, structHandler, true);
            if (type != null)
            {
                nSpace.Types.Add(type);
            }
        }

        private void GenerateProperty(CodeNamespace nSpace, Action<string, StructureType> structHandler, FeatureSpec spec, FeatureProperty featureProperty)
        {
            var propertySpec = spec?.Property?.FirstOrDefault(c => featureProperty.Identifier == (c.Identifier ?? c.Code));
            // ensure dto type can be resolved
            _translationProvider.GetDtoTypeReference(featureProperty.DataType, featureProperty.Identifier, structHandler);
            if (featureProperty.DataType.Item is ConstrainedType)
            {
                _loggingChannel.Debug($"Generating constrained DTO type for property {featureProperty.Identifier}");
                nSpace.Types.Add(GeneratePropertyResponse(featureProperty, propertySpec, structHandler));
            }
        }

        private void GenerateCommand(Feature feature, CodeNamespace nSpace, Action<string, StructureType> structHandler, FeatureSpec spec, FeatureCommand featureCommand)
        {
            _loggingChannel.Debug($"Generating request/response classes for {featureCommand.Identifier}");
            var commandSpec = spec?.Command?.FirstOrDefault(c => featureCommand.Identifier == (c.Identifier ?? c.Code));
            nSpace.Types.Add(GenerateCommandRequestType(feature, featureCommand, commandSpec, structHandler));

            if (featureCommand.IntermediateResponse != null && featureCommand.IntermediateResponse.Length > 0)
            {
                nSpace.Types.Add(GenerateCommandIntermediateType(featureCommand, commandSpec, structHandler));
            }

            if (featureCommand.Response != null && featureCommand.Response.Length > 0)
            {
                nSpace.Types.Add(GenerateCommandResponseType(featureCommand, commandSpec, structHandler));
            }
        }

        private CodeTypeDeclaration GeneratePropertyResponse( FeatureProperty featureProperty, PropertySpec spec, Action<string, StructureType> structHandler )
        {
            var propertyStructure = new StructureType
            {
                Element = new[]
                {
                    new SiLAElement
                    {
                        DataType = featureProperty.DataType,
                        Identifier = "Value"
                    }
                }
            };
            var declaration = GenerateDto( featureProperty.Identifier + "Response", propertyStructure, Encapsulate( spec?.Mapping, featureProperty.Identifier ), false, featureProperty.Identifier, structHandler );
            declaration.WriteDocumentation( $"Data transfer object to encapsulate the response of the {featureProperty.DisplayName} property" );
            return declaration;
        }

        private PropertyMapping[] Encapsulate( TypeMapping mapping, string identifier )
        {
            if(mapping == null)
            {
                return null;
            }
            return new[] { new PropertyMapping
            {
                Identifier = identifier,
                Mapping = mapping,
            }};
        }

        private CodeTypeDeclaration GenerateCommandIntermediateType( FeatureCommand featureCommand, CommandSpec spec, Action<string, StructureType> structHandler )
        {
            var commandResponseStructure = new StructureType
            {
                Element = featureCommand.IntermediateResponse.Select( response => new SiLAElement
                {
                    Identifier = response.Identifier,
                    DataType = response.DataType,
                    DisplayName = response.DisplayName,
                    Description = response.Description
                } ).ToArray()
            };
            PropertyMapping[] mappings = null;
            if(spec?.IntermediateResponse != null)
            {
                mappings = new PropertyMapping[] { spec.IntermediateResponse };
            }
            var declaration = GenerateDto( GetStructureName( NameProvider.GenerateCommandIntermediateType( featureCommand ) ), commandResponseStructure, mappings, featureCommand.IntermediateResponse != null && featureCommand.IntermediateResponse.Length > 1,
                featureCommand.Identifier + ".Intermediate", structHandler );
            declaration.WriteDocumentation( $"Data transfer object for the intermediate response of the {featureCommand.DisplayName} command" );
            return declaration;
        }

        private CodeTypeDeclaration GenerateCommandResponseType( FeatureCommand featureCommand, CommandSpec spec, Action<string, StructureType> structHandler )
        {
            var commandResponseStructure = new StructureType
            {
                Element = featureCommand.Response.Select( response => new SiLAElement
                {
                    Identifier = response.Identifier,
                    DataType = response.DataType,
                    DisplayName = response.DisplayName,
                    Description = response.Description
                } ).ToArray()
            };
            var declaration = GenerateDto( GetStructureName( NameProvider.GenerateCommandResponseType( featureCommand ) ), commandResponseStructure, spec?.Response, featureCommand.Response != null && featureCommand.Response.Length > 1,
                featureCommand.Identifier, structHandler );
            declaration.WriteDocumentation( $"Data transfer object for the response of the {featureCommand.DisplayName} command" );
            return declaration;
        }

        private string GetStructureName( CodeTypeReference typeReference )
        {
            if(!typeReference.BaseType.EndsWith( "Dto" ))
            {
                throw new InvalidOperationException( $"{typeReference.BaseType} does not end with required suffix Dto" );
            }

            return typeReference.BaseType.Substring( 0, typeReference.BaseType.Length - 3 );
        }

        private CodeTypeDeclaration GenerateCommandRequestType( Feature feature, FeatureCommand featureCommand, CommandSpec spec, Action<string, StructureType> structHandler )
        {
            var commandRequestStructure = new StructureType
            {
                Element = featureCommand.Parameter
            };
            var requestType = GenerateDto( GetStructureName( NameProvider.GenerateCommandRequestType( featureCommand ) ), commandRequestStructure, spec?.Parameter, false, featureCommand.Identifier, structHandler );
            requestType.BaseTypes.Add( typeof( ISilaRequestObject ) );
            var commandIdentifierProperty = GenerateCommandIdentifierProperty( feature.GetFullyQualifiedIdentifier( featureCommand ) );
            requestType.Members.Add( commandIdentifierProperty );
            requestType.WriteDocumentation( $"Data transfer object for the request of the {featureCommand.DisplayName} command" );

            return requestType;
        }

        private static CodeMemberProperty GenerateCommandIdentifierProperty( string identifier )
        {
            var commandIdentifierProperty = new CodeMemberProperty()
            {
                Name = nameof( ISilaRequestObject.CommandIdentifier ),
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                Type = new CodeTypeReference( typeof( string ) ),
                HasGet = true,
                HasSet = false
            };
            commandIdentifierProperty.WriteDocumentation( "Gets the command identifier for this command", "The fully qualified command identifier" );
            commandIdentifierProperty.GetStatements.Add( new CodeMethodReturnStatement( new CodePrimitiveExpression( identifier ) ) );
            return commandIdentifierProperty;
        }

        private CodeTypeDeclaration GenerateDataTypeDefinition( SiLAElement dataType, TypeSpec spec, Action<string, StructureType> structHandler, bool encapsulate )
        {
            CodeTypeDeclaration declaration = null;
            if(dataType.DataType.Item is ConstrainedType constrained)
            {
                if(constrained.Constraints.Set != null && constrained.Constraints.Set.Length > 0)
                {
                    return GenerateEnumeration( dataType, constrained.Constraints.Set );
                }
            }
            else if(dataType.DataType.Item is StructureType structure)
            {
                declaration = GenerateDto( CreateProperIdentifier( dataType.Identifier ), structure, spec?.Property, true, dataType.Identifier, structHandler );
                if(encapsulate)
                {
                    declaration = EncapsulateDto( declaration );
                }
            }

            if(declaration == null)
            {
                var dataTypeStructure = new StructureType
                {
                    Element = new[]
                    {
                        new SiLAElement
                        {
                            DataType = dataType.DataType,
                            Identifier = dataType.Identifier,
                            DisplayName = dataType.DisplayName,
                            Description = dataType.Description
                        }
                    }
                };
                declaration = GenerateDto( CreateProperIdentifier( dataType.Identifier ), dataTypeStructure, null, true, dataType.Identifier, structHandler );
            }

            declaration.WriteDocumentation( $"The data transfer object for {dataType.DisplayName}" );
            return declaration;
        }

        private CodeTypeDeclaration EncapsulateDto( CodeTypeDeclaration codeTypeDeclaration )
        {
            var innerType = new CodeTypeDeclaration
            {
                Name = "InnerStruct",
                Attributes = MemberAttributes.Public,
                TypeAttributes = TypeAttributes.Public,
                IsClass = true
            };
            AddAttribute( innerType.CustomAttributes, typeof( ProtoContractAttribute ) );
            innerType.WriteDocumentation( "Represents the inner structure for actual content" );
            var innerField = new CodeMemberField
            {
                Name = "_inner",
                Attributes = MemberAttributes.Private,
                Type = new CodeTypeReference( innerType.Name )
            };
            var innerFieldRef = new CodeFieldReferenceExpression( null, innerField.Name );
            var innerProperty = new CodeMemberProperty
            {
                Name = "Inner",
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                HasGet = true,
                HasSet = true,
                Type = innerField.Type,
                GetStatements =
                {
                    new CodeMethodReturnStatement(innerFieldRef)
                },
                SetStatements =
                {
                    new CodeAssignStatement(innerFieldRef, new CodePropertySetValueReferenceExpression())
                }
            };
            innerProperty.CustomAttributes.AddAttribute( typeof( ProtoMemberAttribute ), 1 );
            innerProperty.WriteDocumentation( "The actual contents of the data transfer object." );
            var innerPropertyRef = new CodePropertyReferenceExpression( null, innerProperty.Name );

            foreach(var constructor in codeTypeDeclaration.Members.OfType<CodeConstructor>().ToArray())
            {
                codeTypeDeclaration.Members.Remove( constructor );
                innerType.Members.Add( constructor );

                var wrapConstructor = new CodeConstructor
                {
                    Attributes = constructor.Attributes
                };
                var createInner = new CodeObjectCreateExpression( innerField.Type );
                foreach(CodeParameterDeclarationExpression parameter in constructor.Parameters)
                {
                    wrapConstructor.Parameters.Add( parameter );
                    createInner.Parameters.Add( new CodeArgumentReferenceExpression( parameter.Name ) );
                }
                wrapConstructor.Statements.Add( new CodeAssignStatement( innerFieldRef, createInner ) );
                CopyComments( constructor.Comments, wrapConstructor.Comments );
                codeTypeDeclaration.Members.Add( wrapConstructor );
            }

            foreach(var field in codeTypeDeclaration.Members.OfType<CodeMemberField>().ToArray())
            {
                codeTypeDeclaration.Members.Remove( field );
                innerType.Members.Add( field );
            }

            var ifNullCreate = new CodeConditionStatement(
                new CodeBinaryOperatorExpression( innerFieldRef, CodeBinaryOperatorType.IdentityEquality, new CodePrimitiveExpression() ),
                new CodeAssignStatement( innerFieldRef, new CodeObjectCreateExpression( innerField.Type ) ) );

            foreach(var property in codeTypeDeclaration.Members.OfType<CodeMemberProperty>().ToArray())
            {
                codeTypeDeclaration.Members.Remove( property );
                innerType.Members.Add( property );
                var wrapProperty = new CodeMemberProperty
                {
                    Name = property.Name,
                    Attributes = property.Attributes,
                    Type = property.Type,
                    GetStatements =
                    {
                        ifNullCreate,
                        new CodeMethodReturnStatement(new CodePropertyReferenceExpression(innerPropertyRef, property.Name))
                    },
                    SetStatements =
                    {
                        ifNullCreate,
                        new CodeAssignStatement(new CodePropertyReferenceExpression(innerPropertyRef, property.Name), new CodePropertySetValueReferenceExpression())
                    }
                };
                CopyComments( property.Comments, wrapProperty.Comments );
                codeTypeDeclaration.Members.Add( wrapProperty );
            }

            codeTypeDeclaration.Members.Add( innerField );
            codeTypeDeclaration.Members.Add( innerProperty );
            codeTypeDeclaration.Members.Add( innerType );
            return codeTypeDeclaration;
        }

        private static void CopyComments( CodeCommentStatementCollection source, CodeCommentStatementCollection target )
        {
            for(int i = 0; i < source.Count; i++)
            {
                target.Add( source[i] );
            }
        }

        private static string CreateProperIdentifier( string identifier )
        {
            if(identifier.All( char.IsUpper ))
            {
                identifier = identifier[0] + identifier.Substring( 1 ).ToLowerInvariant();
            }

            return identifier;
        }

        private CodeTypeDeclaration GenerateEnumeration( SiLAElement dataType, string[] enumeration )
        {
            var dto = new CodeTypeDeclaration( dataType.Identifier + "Dto" )
            {
                Attributes = MemberAttributes.Public,
                TypeAttributes = TypeAttributes.Public,
                IsClass = true
            };
            AddAttribute( dto.CustomAttributes, typeof( ProtoContractAttribute ) );
            var innerType = new CodeTypeReference( dataType.Identifier );
            dto.BaseTypes.Add( new CodeTypeReference( typeof( ISilaTransferObject<> ).FullName, innerType ) );
            var (constructor, innerRef) = GenerateExtractionConstructors( dto, innerType );

            var valueRef = GenerateProperty( new SiLAElement()
            {
                Identifier = "Value",
                DisplayName = "Value",
                Description = "The string representation of the given enum member",
                DataType = new DataTypeType()
                {
                    Item = BasicType.String
                }
            }, 1, dto, null, null );
            constructor.Statements.Add( new CodeAssignStatement( valueRef, new CodeMethodInvokeExpression( innerRef, nameof( object.ToString ) ) ) );
            var extractMethod = new CodeMemberMethod()
            {
                Name = nameof( ISilaTransferObject<object>.Extract ),
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                ReturnType = innerType
            };
            dto.Members.Add( extractMethod );
            extractMethod.Parameters.Add( new CodeParameterDeclarationExpression( typeof( IBinaryStore ), BinaryStoreParameterName ) );
            extractMethod.Statements.Add( new CodeMethodReturnStatement(
                new CodeCastExpression( innerType, new CodeMethodInvokeExpression( new CodeTypeReferenceExpression( typeof( Enum ) ), nameof( Enum.Parse ),
                new CodeTypeOfExpression( innerType ), new CodePropertyReferenceExpression( valueRef, "Value" ) ) ) ) );
            extractMethod.WriteDocumentation( "Extracts the transferred value", "the inner value", new Dictionary<string, string>()
            {
                { BinaryStoreParameterName, "The binary store in which to store binary data" }
            } );

            GenerateStaticCreateMethod( dto, innerType );

            var (validationMethod, errors) = GenerateGetValidationErrors();
            CompleteGetValidationErrors( validationMethod, errors );
            dto.Members.Add( validationMethod );
            dto.WriteDocumentation( $"The data transfer object for the {dataType.DisplayName} enumeration" );
            return dto;
        }

        private CodeTypeDeclaration GenerateDto( string name, StructureType structureType, PropertyMapping[] mappings, bool isSilaTransferObject, string identifier, Action<string, StructureType> structHandler )
        {
            var dto = new CodeTypeDeclaration( name + "Dto" )
            {
                Attributes = MemberAttributes.Public,
                TypeAttributes = TypeAttributes.Public,
                IsClass = true
            };
            AddAttribute( dto.CustomAttributes, typeof( ProtoContractAttribute ) );
            var elementAction = isSilaTransferObject
                ? ImplementTransferObjectDto( name, dto, identifier )
                : ImplementRequestResponseDto( identifier, dto );
            ImplementDtoProperties( structureType, mappings, dto, elementAction, structHandler, identifier );
            return dto;
        }

        private Action<SiLAElement, Expression, CodePropertyReferenceExpression> ImplementRequestResponseDto( string methodIdentifier,
            CodeTypeDeclaration dto )
        {
            dto.BaseTypes.Add( typeof( ISilaTransferObject ) );
            var (constructor, storeRef) = GenerateMethodDtoConstructors( dto );

            var validateMethod = new CodeMemberMethod
            {
                Name = nameof( Argument.Validate ),
                Attributes = MemberAttributes.Public | MemberAttributes.Final
            };
            validateMethod.WriteDocumentation( "Validates the given request object" );
            dto.Members.Add( validateMethod );

            var validate = new CodeMethodReferenceExpression(
                new CodeTypeReferenceExpression( nameof( Argument ) ),
                nameof( Argument.Validate ) );

            void ElementAction( SiLAElement element, Expression expression, CodePropertyReferenceExpression property )
            {
                var type = NameProvider.GetMemberType( FollowIdentifier( methodIdentifier + "." + element.Identifier, expression ) ) ?? _translationProvider.ExtractType( element.DataType, methodIdentifier + element.Identifier );
                var parameter = new CodeParameterDeclarationExpression( type, element.Identifier.ToCamelCase() );
                constructor.Parameters.Insert( constructor.Parameters.Count - 1, parameter );
                constructor.Statements.Add( new CodeAssignStatement( property, _translationProvider.EncapsulateAsDto( new CodeArgumentReferenceExpression( parameter.Name ), element.DataType, storeRef, methodIdentifier + element.Identifier ) ) );
                if(!string.IsNullOrEmpty( element.Description ))
                {
                    constructor.Comments.Add( new CodeCommentStatement( $@"<param name=""{parameter.Name}"">{element.Description}</param>", true ) );
                }
                validateMethod.Statements.Add( new CodeMethodInvokeExpression( validate, property, new CodePrimitiveExpression( parameter.Name ) ) );
                if(element.DataType.Item is ConstrainedType constrained)
                {
                    foreach(var validationGenerator in _validators)
                    {
                        foreach(var validation in validationGenerator.CreateValidation( property, constrained.DataType, constrained.Constraints, dto ))
                        {
                            validateMethod.Statements.Add( new CodeConditionStatement(
                                validation.CheckExpression,
                                new CodeThrowExceptionStatement( new CodeObjectCreateExpression( nameof( ArgumentException ),
                                    validation.ErrorMessage,
                                    new CodePrimitiveExpression( parameter.Name ) ) ) ) );
                        }
                    }
                }
            }

            return ElementAction;
        }

        private Action<SiLAElement, Expression, CodePropertyReferenceExpression> ImplementTransferObjectDto( string name,
            CodeTypeDeclaration dto, string identifier )
        {
            var innerType = NameProvider.GetMemberType( name ) ?? _translationProvider.ExtractType( new DataTypeType() { Item = name }, null );
            dto.BaseTypes.Add( new CodeTypeReference( typeof( ISilaTransferObject<> ).FullName, innerType ) );
            var (constructor, innerRef) = GenerateExtractionConstructors( dto, innerType );
            var extracted = GenerateExtraction( dto, innerType );
            var storeRef = new CodeArgumentReferenceExpression( BinaryStoreParameterName );

            void ElementAction( SiLAElement element, Expression expression, CodePropertyReferenceExpression property )
            {
                var correspondingPropertyName = CreateProperIdentifier( property.PropertyName ) == name ? "Value" : property.PropertyName;
                if(correspondingPropertyName == nameof( ISilaRequestObject.CommandIdentifier ) + "_")
                {
                    correspondingPropertyName = nameof( ISilaRequestObject.CommandIdentifier );
                }
                var customType = NameProvider.GetMemberType( FollowIdentifier( name + "." + element.Identifier, expression ) );
                extracted.Add( _translationProvider.Extract( property, element.DataType, new CodeArgumentReferenceExpression( BinaryStoreParameterName ), customType ) );
                constructor.Statements.Add( new CodeAssignStatement( property,
                    _translationProvider.EncapsulateAsDto( new CodePropertyReferenceExpression( innerRef, correspondingPropertyName ),
                        element.DataType, storeRef, identifier + element.Identifier ) ) );
            }

            GenerateStaticCreateMethod( dto, innerType );
            return ElementAction;
        }

        private string FollowIdentifier( string identifier, Expression expression )
        {
            if(expression is PropertyExpression propertyExpression)
            {
                return identifier + "." + propertyExpression.Property;
            }
            return identifier;
        }

        private void ImplementDtoProperties( StructureType structureType, PropertyMapping[] mappings, CodeTypeDeclaration dto,
            Action<SiLAElement, Expression, CodePropertyReferenceExpression> elementAction, Action<string, StructureType> structHandler, string parentIdentifier )
        {
            var (validationMethod, errors) = GenerateGetValidationErrors();
            var index = 1;
            if(structureType.Element != null)
            {
                var errorsRef = new CodeVariableReferenceExpression( errors.Name );
                var requireCall = new CodeMethodReferenceExpression(
                    new CodeTypeReferenceExpression( nameof( Argument ) ),
                    nameof( Argument.Require ) );
                foreach(var element in structureType.Element)
                {
                    var mapping = mappings?.FirstOrDefault( m => string.Equals( element.Identifier, m.Identifier ?? m.Key, StringComparison.OrdinalIgnoreCase ) );
                    var property = GenerateProperty( element, index, dto, structHandler, parentIdentifier );
                    elementAction( element, mapping?.Mapping?.ValueExpression, property );
                    if(element.DataType.Item is ConstrainedType constraints)
                    {
                        ImplementValidationErrorConstraints( constraints.Constraints, constraints.DataType, dto, validationMethod.Statements, errorsRef, property );
                    }
                    var validate = new CodeMethodInvokeExpression( requireCall, property, new CodePrimitiveExpression( element.Identifier.ToCamelCase() ) );
                    errors.InitExpression = new CodeBinaryOperatorExpression( errors.InitExpression, CodeBinaryOperatorType.Add, validate );

                    index++;
                }
            }

            CompleteGetValidationErrors( validationMethod, errors );
            dto.Members.Add( validationMethod );
        }

        private (CodeConstructor, CodeExpression) GenerateMethodDtoConstructors( CodeTypeDeclaration dto )
        {
            // add default constructor
            var defaultConstructor = new CodeConstructor() { Attributes = MemberAttributes.Public };
            dto.Members.Add( defaultConstructor );
            defaultConstructor.WriteDocumentation( "Create a new instance" );
            var copyConstructor = new CodeConstructor() { Attributes = MemberAttributes.Public };
            copyConstructor.WriteDocumentation( "Create a new instance", parameters: new Dictionary<string, string>()
            {
                { BinaryStoreParameterName, "An object to organize binaries." }
            } );
            copyConstructor.Parameters.Add( new CodeParameterDeclarationExpression( typeof( IBinaryStore ), BinaryStoreParameterName ) );
            var storeRef = new CodeArgumentReferenceExpression( BinaryStoreParameterName );
            dto.Members.Add( copyConstructor );
            return (copyConstructor, storeRef);
        }

        private void GenerateStaticCreateMethod( CodeTypeDeclaration dto, CodeTypeReference innerType )
        {
            var createMethod = new CodeMemberMethod
            {
                Name = "Create",
                Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static,
                ReturnType = new CodeTypeReference( dto.Name )
            };
            var inner = new CodeParameterDeclarationExpression( innerType, "inner" );
            var store = new CodeParameterDeclarationExpression( typeof( IBinaryStore ), BinaryStoreParameterName );
            createMethod.Parameters.Add( inner );
            createMethod.Parameters.Add( store );
            createMethod.WriteDocumentation( "Creates the data transfer object from the given object to transport",
                parameters: new Dictionary<string, string>()
                {
                    { inner.Name, "The object to transfer" },
                    {BinaryStoreParameterName, "An object to store binary data" }
                } );
            var innerRef = new CodeArgumentReferenceExpression( inner.Name );
            var storeRef = new CodeArgumentReferenceExpression( store.Name );
            createMethod.Statements.Add(
                new CodeMethodReturnStatement( new CodeObjectCreateExpression( createMethod.ReturnType, innerRef, storeRef ) ) );
            dto.Members.Add( createMethod );
        }

        private void CompleteGetValidationErrors( CodeMemberMethod validationMethod,
            CodeVariableDeclarationStatement errors )
        {
            if(validationMethod.Statements.Count == 1)
            {
                validationMethod.Statements.Clear();
                if(errors.InitExpression is CodePrimitiveExpression)
                {
                    validationMethod.Statements.Add( new CodeMethodReturnStatement( new CodePrimitiveExpression( null ) ) );
                }
                else
                {
                    validationMethod.Statements.Add( new CodeMethodReturnStatement( errors.InitExpression ) );
                }
            }
            else
            {
                validationMethod.Statements.Add( new CodeMethodReturnStatement( new CodeVariableReferenceExpression( errors.Name ) ) );
            }
        }

        private CodeExpressionCollection GenerateExtraction( CodeTypeDeclaration dto, CodeTypeReference innerType )
        {
            var extractMethod = new CodeMemberMethod()
            {
                Name = nameof( ISilaTransferObject<object>.Extract ),
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                ReturnType = innerType
            };
            dto.Members.Add( extractMethod );
            extractMethod.Parameters.Add( new CodeParameterDeclarationExpression( typeof( IBinaryStore ), BinaryStoreParameterName ) );
            extractMethod.WriteDocumentation( "Extracts the transferred value", "the inner value", new Dictionary<string, string>()
            {
                { BinaryStoreParameterName, "The binary store in which to store binary data" }
            } );
            var constructorMethod = NameProvider?.GetStaticConstructorMethod( innerType.BaseType );
            if(constructorMethod != null)
            {
                var createReturn =
                    new CodeMethodInvokeExpression( new CodeTypeReferenceExpression( innerType ), constructorMethod );
                extractMethod.Statements.Add( new CodeMethodReturnStatement( createReturn ) );
                return createReturn.Parameters;
            }
            else
            {
                var createReturn = new CodeObjectCreateExpression( innerType );
                extractMethod.Statements.Add( new CodeMethodReturnStatement( createReturn ) );
                return createReturn.Parameters;
            }
        }

        private (CodeConstructor, CodeArgumentReferenceExpression) GenerateExtractionConstructors(
            CodeTypeDeclaration dto, CodeTypeReference innerType )
        {
            // add default constructor
            var defaultConstructor = new CodeConstructor() { Attributes = MemberAttributes.Public };
            defaultConstructor.WriteDocumentation( "Initializes a new instance (to be used by the serializer)" );
            dto.Members.Add( defaultConstructor );
            var copyConstructor = new CodeConstructor() { Attributes = MemberAttributes.Public };
            dto.Members.Add( copyConstructor );
            var parameter = new CodeParameterDeclarationExpression( innerType, "inner" );
            copyConstructor.Parameters.Add( parameter );
            copyConstructor.Parameters.Add( new CodeParameterDeclarationExpression( typeof( IBinaryStore ), BinaryStoreParameterName ) );
            copyConstructor.WriteDocumentation( "Initializes a new data transfer object from the business object",
                parameters: new Dictionary<string, string>()
                {
                    {parameter.Name, "The business object that should be transferred"},
                    {BinaryStoreParameterName, "A component to handle binary data"}
                } );
            return (copyConstructor, new CodeArgumentReferenceExpression( parameter.Name ));
        }

        private (CodeMemberMethod, CodeVariableDeclarationStatement) GenerateGetValidationErrors()
        {
            var method = new CodeMemberMethod()
            {
                Name = nameof( ISilaTransferObject.GetValidationErrors ),
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                ReturnType = new CodeTypeReference( typeof( string ) )
            };
            method.WriteDocumentation( "Validates the contents of this transfer object", "A validation error or null, if no validation error occurred." );
            var variableDeclaration =
                new CodeVariableDeclarationStatement( typeof( string ), "errors",
                    new CodePrimitiveExpression( string.Empty ) );
            method.Statements.Add( variableDeclaration );
            return (method, variableDeclaration);
        }

        private CodePropertyReferenceExpression GenerateProperty( SiLAElement element, int index,
            CodeTypeDeclaration dto, Action<string, StructureType> structHandler, string parentIdentifier )
        {
            var property = new CodeMemberProperty
            {
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                Name = element.Identifier,
                HasSet = true,
                HasGet = true,
                Type = _translationProvider.GetDtoTypeReference( element.DataType, parentIdentifier + element.Identifier, structHandler )
            };
            if(element.Identifier == nameof( ISilaRequestObject.CommandIdentifier ))
            {
                property.Name += "_";
            }
            property.WriteDocumentation( element.Description ?? $"The {element.DisplayName} property" );
            AddAttribute( property.CustomAttributes, typeof( ProtoMemberAttribute ), index );
            dto.Members.Add( property );
            var field = new CodeMemberField
            {
                Name = "_" + property.Name.ToCamelCase(),
                Attributes = MemberAttributes.Private,
                Type = property.Type
            };
            dto.Members.Add( field );
            var fieldRef = new CodeFieldReferenceExpression( null, field.Name );
            property.GetStatements.Add( new CodeMethodReturnStatement( fieldRef ) );
            property.SetStatements.Add( new CodeAssignStatement( fieldRef,
                new CodePropertySetValueReferenceExpression() ) );
            return new CodePropertyReferenceExpression( null, property.Name );
        }

        private void ImplementValidationErrorConstraints( Constraints constraints, DataTypeType dataType, CodeTypeDeclaration wrapper,
            CodeStatementCollection statements, CodeVariableReferenceExpression resultRef,
            CodePropertyReferenceExpression propertyReference )
        {
            try
            {
                foreach(var validator in _validators)
                {
                    foreach(var validation in validator.CreateValidation( propertyReference, dataType, constraints, wrapper ))
                    {
                        statements.Add( CreateValidityStatement(validation.CheckExpression, validation.ErrorMessage, resultRef));
                    }
                }
            }
            catch(Exception ex)
            {
                _loggingChannel.Warn( "Exception trying to generate code to enforce constraints", ex );
            }
        }

        private CodeStatement CreateValidityStatement(CodeExpression condition, CodeExpression errorMessage,
            CodeVariableReferenceExpression resultRef)
        {
            return new CodeConditionStatement( condition,
                new CodeAssignStatement( resultRef,
                    new CodeBinaryOperatorExpression( resultRef, CodeBinaryOperatorType.Add, errorMessage ) ) );
        }

        private void AddAttribute( CodeAttributeDeclarationCollection attributes, Type attributeType,
            params object[] values )
        {
            var declaration = new CodeAttributeDeclaration( new CodeTypeReference( attributeType ),
                values.Select( o => new CodeAttributeArgument( o is CodeExpression exp ? exp : new CodePrimitiveExpression( o ) ) )
                      .ToArray() );

            attributes.Add( declaration );
        }
    }
}

#pragma warning restore S3265 // Non-flags enums should not be used in bitwise operations
