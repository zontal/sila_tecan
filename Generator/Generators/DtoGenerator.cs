using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ProtoBuf;
using Tecan.Sila2.Binary;
using Tecan.Sila2.DynamicClient;
using Tecan.Sila2.Generator.Contracts;
using Tecan.Sila2.Generator.Helper;

namespace Tecan.Sila2.Generator.Generators
{
    /// <summary>
    /// Generates data transfer objects
    /// </summary>
    [Export(typeof(IDtoGenerator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class DtoGenerator : IDtoGenerator
    {
        private const string BinaryStoreParameterName = "store";

        /// <summary>
        /// The name provider for the Dto generator
        /// </summary>
        public ICodeNameProvider NameProvider { get; }

        private readonly ITypeTranslationProvider _translationProvider;

        /// <summary>
        /// Creates a new Dto generator for the given name provider
        /// </summary>
        /// <param name="nameProvider"></param>
        [ImportingConstructor]
        public DtoGenerator(ITypeTranslationProvider translationProvider, ICodeNameProvider nameProvider)
        {
            _translationProvider = translationProvider;
            NameProvider = nameProvider;
        }

        /// <inheritdoc />
        public CodeCompileUnit GenerateInterfaceUnit(Feature feature, string ns)
        {
            var unit = new CodeCompileUnit();
            var nSpace = new CodeNamespace(ns);
            unit.Namespaces.Add(nSpace);
            nSpace.Imports.Add(new CodeNamespaceImport("Tecan.Sila2"));

            var structures = new Dictionary<string, StructureType>();
            var structHandler = new Action<string, StructureType>( ( name, structure ) =>
            {
                if( !structures.ContainsKey( name ) )
                {
                    structures.Add(name, structure);
                }
            } );

            foreach (var featureCommand in feature.Items.OfType<FeatureCommand>())
            {
                nSpace.Types.Add(GenerateCommandRequestType(feature, featureCommand, structHandler ) );

                if (featureCommand.IntermediateResponse != null && featureCommand.IntermediateResponse.Length > 0)
                {
                    nSpace.Types.Add(GenerateCommandIntermediateType(featureCommand, structHandler ) );
                }

                if (featureCommand.Response != null && featureCommand.Response.Length > 0)
                {
                    nSpace.Types.Add(GenerateCommandResponseType(featureCommand, structHandler ) );
                }
            }

            foreach (var featureProperty in feature.Items.OfType<FeatureProperty>())
            {
                // ensure dto type can be resolved
                _translationProvider.GetDtoTypeReference( featureProperty.DataType, featureProperty.Identifier, structHandler );
                if (featureProperty.DataType.Item is ConstrainedType)
                {
                    nSpace.Types.Add(GeneratePropertyResponse(featureProperty, structHandler ) );
                }
            }

            foreach (var dataTypeDefinition in feature.Items.OfType<SiLAElement>())
            {
                var type = GenerateDataTypeDefinition(dataTypeDefinition, structHandler );
                if (type != null)
                {
                    nSpace.Types.Add(type);
                }
            }

            foreach( var structure in structures )
            {
                var element = new SiLAElement()
                {
                    Identifier = structure.Key,
                    DataType = new DataTypeType()
                    {
                        Item = structure.Value
                    },
                    DisplayName = structure.Key
                };
                var type = GenerateDataTypeDefinition( element, null );
                if( type != null )
                {
                    nSpace.Types.Add( type );
                }
            }

            return unit;
        }

        private CodeTypeDeclaration GeneratePropertyResponse(FeatureProperty featureProperty, Action<string, StructureType> structHandler )
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
            var declaration = GenerateDto(featureProperty.Identifier + "Response", propertyStructure, false, featureProperty.Identifier, structHandler);
            declaration.WriteDocumentation($"Data transfer object to encapsulate the response of the {featureProperty.DisplayName} property");
            return declaration;
        }

        private CodeTypeDeclaration GenerateCommandIntermediateType(FeatureCommand featureCommand, Action<string, StructureType> structHandler )
        {
            var commandResponseStructure = new StructureType
            {
                Element = featureCommand.IntermediateResponse.Select(response => new SiLAElement
                {
                    Identifier = response.Identifier,
                    DataType = response.DataType,
                    DisplayName = response.DisplayName,
                    Description = response.Description
                }).ToArray()
            };
            var declaration = GenerateDto( GetStructureName( NameProvider.GenerateCommandIntermediateType( featureCommand ) ), commandResponseStructure, featureCommand.IntermediateResponse != null && featureCommand.IntermediateResponse.Length > 1,
                featureCommand.Identifier + ".Intermediate", structHandler);
            declaration.WriteDocumentation($"Data transfer object for the intermediate response of the {featureCommand.DisplayName} command");
            return declaration;
        }

        private CodeTypeDeclaration GenerateCommandResponseType(FeatureCommand featureCommand, Action<string, StructureType> structHandler )
        {
            var commandResponseStructure = new StructureType
            {
                Element = featureCommand.Response.Select(response => new SiLAElement
                {
                    Identifier = response.Identifier,
                    DataType = response.DataType,
                    DisplayName = response.DisplayName,
                    Description = response.Description
                }).ToArray()
            };
            var declaration = GenerateDto(GetStructureName(NameProvider.GenerateCommandResponseType(featureCommand)), commandResponseStructure, featureCommand.Response != null && featureCommand.Response.Length > 1,
                featureCommand.Identifier, structHandler);
            declaration.WriteDocumentation($"Data transfer object for the response of the {featureCommand.DisplayName} command");
            return declaration;
        }

        private string GetStructureName( CodeTypeReference typeReference )
        {
            if( !typeReference.BaseType.EndsWith( "Dto" ) )
            {
                throw new InvalidOperationException($"{typeReference.BaseType} does not end with required suffix Dto");
            }

            return typeReference.BaseType.Substring( 0, typeReference.BaseType.Length - 3 );
        }

        private CodeTypeDeclaration GenerateCommandRequestType(Feature feature, FeatureCommand featureCommand, Action<string, StructureType> structHandler )
        {
            var commandRequestStructure = new StructureType
            {
                Element = featureCommand.Parameter
            };
            var requestType = GenerateDto( GetStructureName( NameProvider.GenerateCommandRequestType( featureCommand ) ), commandRequestStructure, false, featureCommand.Identifier, structHandler);
            requestType.BaseTypes.Add(typeof(ISilaRequestObject));
            var commandIdentifierProperty = GenerateCommandIdentifierProperty(feature.GetFullyQualifiedIdentifier(featureCommand));
            requestType.Members.Add(commandIdentifierProperty);
            requestType.WriteDocumentation($"Data transfer object for the request of the {featureCommand.DisplayName} command");
            return requestType;
        }

        private static CodeMemberProperty GenerateCommandIdentifierProperty(string identifier)
        {
            var commandIdentifierProperty = new CodeMemberProperty()
            {
                Name = nameof(ISilaRequestObject.CommandIdentifier),
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                Type = new CodeTypeReference(typeof(string)),
                HasGet = true,
                HasSet = false
            };
            commandIdentifierProperty.WriteDocumentation("Gets the command identifier for this command", "The fully qualified command identifier");
            commandIdentifierProperty.GetStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(identifier)));
            return commandIdentifierProperty;
        }

        private CodeTypeDeclaration GenerateDataTypeDefinition( SiLAElement dataType, Action<string, StructureType> structHandler )
        {
            if( dataType.DataType.Item is ConstrainedType constrained )
            {
                if( constrained.Constraints.Set != null && constrained.Constraints.Set.Length > 0 )
                {
                    return GenerateEnumeration( dataType, constrained.Constraints.Set );
                }
            }
            else if( dataType.DataType.Item is StructureType structure )
            {
                return GenerateDto( CreateProperIdentifier( dataType.Identifier ), structure, true, dataType.Identifier, structHandler );
            }

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
            var declaration = GenerateDto( CreateProperIdentifier( dataType.Identifier ), dataTypeStructure, true, dataType.Identifier, structHandler );
            declaration.WriteDocumentation( $"The data transfer object for {dataType.DisplayName}" );
            return declaration;
        }

        private static string CreateProperIdentifier(string identifier)
        {
            if (identifier.All(char.IsUpper))
            {
                identifier = identifier[0] + identifier.Substring(1).ToLowerInvariant();
            }

            return identifier;
        }

        private CodeTypeDeclaration GenerateEnumeration(SiLAElement dataType, string[] enumeration)
        {
            var dto = new CodeTypeDeclaration(dataType.Identifier + "Dto")
            {
                Attributes = MemberAttributes.Public,
                TypeAttributes = TypeAttributes.Public,
                IsClass = true
            };
            AddAttribute(dto.CustomAttributes, typeof(ProtoContractAttribute));
            var innerType = new CodeTypeReference(dataType.Identifier);
            dto.BaseTypes.Add(new CodeTypeReference(typeof(ISilaTransferObject<>).FullName, innerType));
            var (constructor, innerRef) = GenerateExtractionConstructors(dto, innerType);

            var valueRef = GenerateProperty(new SiLAElement()
            {
                Identifier = "Value",
                DisplayName = "Value",
                Description = "The string representation of the given enum member",
                DataType = new DataTypeType()
                {
                    Item = BasicType.String
                }
            }, 1, dto, null);
            constructor.Statements.Add(new CodeAssignStatement(valueRef, new CodeMethodInvokeExpression(innerRef, nameof(object.ToString))));
            var extractMethod = new CodeMemberMethod()
            {
                Name = nameof(ISilaTransferObject<object>.Extract),
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                ReturnType = innerType
            };
            dto.Members.Add(extractMethod);
            extractMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(IBinaryStore), BinaryStoreParameterName));
            extractMethod.Statements.Add(new CodeMethodReturnStatement(
                new CodeCastExpression(innerType, new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(Enum)), nameof(Enum.Parse),
                new CodeTypeOfExpression(innerType), new CodePropertyReferenceExpression(valueRef, "Value")))));
            extractMethod.WriteDocumentation("Extracts the transferred value", "the inner value", new Dictionary<string, string>()
            {
                { BinaryStoreParameterName, "The binary store in which to store binary data" }
            });

            GenerateStaticCreateMethod(dto, innerType);

            var (validationMethod, errors) = GenerateGetValidationErrors();
            CompleteGetValidationErrors(validationMethod, errors);
            dto.Members.Add(validationMethod);
            dto.WriteDocumentation($"The data transfer object for the {dataType.DisplayName} enumeration");
            return dto;
        }

        private CodeTypeDeclaration GenerateDto(string name, StructureType structureType, bool isSilaTransferObject, string identifier, Action<string, StructureType> structHandler )
        {
            var dto = new CodeTypeDeclaration( name + "Dto")
            {
                Attributes = MemberAttributes.Public,
                TypeAttributes = TypeAttributes.Public,
                IsClass = true
            };
            AddAttribute(dto.CustomAttributes, typeof(ProtoContractAttribute));
            var elementAction = isSilaTransferObject
                ? ImplementTransferObjectDto(name, dto)
                : ImplementRequestResponseDto(identifier, dto);
            ImplementDtoProperties(structureType, dto, elementAction, structHandler);
            return dto;
        }

        private Action<SiLAElement, CodePropertyReferenceExpression> ImplementRequestResponseDto(string methodIdentifier,
            CodeTypeDeclaration dto)
        {
            dto.BaseTypes.Add(typeof(ISilaTransferObject));
            var (constructor, storeRef) = GenerateMethodDtoConstructors(dto);

            void ElementAction(SiLAElement element, CodePropertyReferenceExpression property)
            {
                var type = NameProvider.GetMemberType( methodIdentifier + "." + element.Identifier ) ?? _translationProvider.ExtractType( element.DataType, element.Identifier );
                var parameter = new CodeParameterDeclarationExpression(type, element.Identifier.ToCamelCase());
                constructor.Parameters.Insert(constructor.Parameters.Count - 1, parameter);
                constructor.Statements.Add(new CodeAssignStatement(property, _translationProvider.EncapsulateAsDto(new CodeArgumentReferenceExpression(parameter.Name), element.DataType, storeRef, element.Identifier)));
                constructor.Comments.Add( new CodeCommentStatement( $@" <param name=""{parameter.Name}"">{element.Description}</param>", true ) );
            }

            return ElementAction;
        }

        private Action<SiLAElement, CodePropertyReferenceExpression> ImplementTransferObjectDto(string name,
            CodeTypeDeclaration dto)
        {
            var innerType = _translationProvider.ExtractType( new DataTypeType() { Item = name }, null );
            dto.BaseTypes.Add(new CodeTypeReference(typeof(ISilaTransferObject<>).FullName, innerType));
            var (constructor, innerRef) = GenerateExtractionConstructors(dto, innerType);
            var extracted = GenerateExtraction(dto, innerType);
            var storeRef = new CodeArgumentReferenceExpression(BinaryStoreParameterName);

            void ElementAction(SiLAElement element, CodePropertyReferenceExpression property)
            {
                var correspondingPropertyName = CreateProperIdentifier(property.PropertyName) == name ? "Value" : property.PropertyName;
                if( correspondingPropertyName == nameof(ISilaRequestObject.CommandIdentifier) + "_" )
                {
                    correspondingPropertyName = nameof(ISilaRequestObject.CommandIdentifier);
                }
                var customType = NameProvider.GetMemberType( name + "." + element.Identifier );
                extracted.Add( _translationProvider.Extract(property, element.DataType, new CodeArgumentReferenceExpression( BinaryStoreParameterName ), customType) );
                constructor.Statements.Add(new CodeAssignStatement(property,
                    _translationProvider.EncapsulateAsDto(new CodePropertyReferenceExpression(innerRef, correspondingPropertyName),
                        element.DataType, storeRef, element.Identifier)));
            }

            GenerateStaticCreateMethod(dto, innerType);
            return ElementAction;
        }


        private void ImplementDtoProperties(StructureType structureType, CodeTypeDeclaration dto,
            Action<SiLAElement, CodePropertyReferenceExpression> elementAction, Action<string, StructureType> structHandler)
        {
            var (validationMethod, errors) = GenerateGetValidationErrors();
            var index = 1;
            if (structureType.Element != null)
            {
                foreach (var element in structureType.Element)
                {
                    var property = GenerateProperty(element, index, dto, structHandler);
                    elementAction(element, property);
                    if (element.DataType.Item is ConstrainedType constraints)
                    {
                        ImplementConstraints(constraints.Constraints, dto, validationMethod.Statements, errors, property);
                    }

                    index++;
                }
            }

            CompleteGetValidationErrors(validationMethod, errors);
            dto.Members.Add(validationMethod);
        }

        private (CodeConstructor, CodeExpression) GenerateMethodDtoConstructors(CodeTypeDeclaration dto)
        {
            // add default constructor
            var defaultConstructor = new CodeConstructor() { Attributes = MemberAttributes.Public };
            dto.Members.Add(defaultConstructor);
            defaultConstructor.WriteDocumentation( "Create a new instance" );
            var copyConstructor = new CodeConstructor() {Attributes = MemberAttributes.Public};
            copyConstructor.WriteDocumentation( "Create a new instance", parameters: new Dictionary<string, string>()
            {
                { BinaryStoreParameterName, "An object to organize binaries." }
            } );
            copyConstructor.Parameters.Add(new CodeParameterDeclarationExpression(typeof(IBinaryStore), BinaryStoreParameterName));
            var storeRef = new CodeArgumentReferenceExpression(BinaryStoreParameterName);
            dto.Members.Add(copyConstructor);
            return (copyConstructor, storeRef);
        }

        private void GenerateStaticCreateMethod(CodeTypeDeclaration dto, CodeTypeReference innerType)
        {
            var createMethod = new CodeMemberMethod
            {
                Name = "Create",
                Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static,
                ReturnType = new CodeTypeReference(dto.Name)
            };
            var inner = new CodeParameterDeclarationExpression(innerType, "inner");
            var store = new CodeParameterDeclarationExpression(typeof(IBinaryStore), BinaryStoreParameterName);
            createMethod.Parameters.Add(inner);
            createMethod.Parameters.Add(store);
            createMethod.WriteDocumentation("Creates the data transfer object from the given object to transport",
                parameters: new Dictionary<string, string>()
                {
                    { inner.Name, "The object to transfer" },
                    {BinaryStoreParameterName, "An object to store binary data" }
                });
            var innerRef = new CodeArgumentReferenceExpression(inner.Name);
            var storeRef = new CodeArgumentReferenceExpression(store.Name);
            createMethod.Statements.Add(
                new CodeMethodReturnStatement(new CodeObjectCreateExpression(createMethod.ReturnType, innerRef, storeRef)));
            dto.Members.Add(createMethod);
        }

        private void CompleteGetValidationErrors(CodeMemberMethod validationMethod,
            CodeVariableReferenceExpression errors)
        {
            if (validationMethod.Statements.Count == 1)
            {
                validationMethod.Statements.Clear();
                validationMethod.Statements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(null)));
            }
            else
            {
                validationMethod.Statements.Add(new CodeMethodReturnStatement(errors));
            }
        }

        private CodeExpressionCollection GenerateExtraction(CodeTypeDeclaration dto, CodeTypeReference innerType)
        {
            var extractMethod = new CodeMemberMethod()
            {
                Name = nameof(ISilaTransferObject<object>.Extract),
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                ReturnType = innerType
            };
            dto.Members.Add(extractMethod);
            extractMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(IBinaryStore), BinaryStoreParameterName));
            extractMethod.WriteDocumentation( "Extracts the transferred value", "the inner value", new Dictionary<string, string>()
            {
                { BinaryStoreParameterName, "The binary store in which to store binary data" }
            } );
            var constructorMethod = NameProvider?.GetStaticConstructorMethod(innerType.BaseType);
            if (constructorMethod != null)
            {
                var createReturn =
                    new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(innerType), constructorMethod);
                extractMethod.Statements.Add(new CodeMethodReturnStatement(createReturn));
                return createReturn.Parameters;
            }
            else
            {
                var createReturn = new CodeObjectCreateExpression(innerType);
                extractMethod.Statements.Add(new CodeMethodReturnStatement(createReturn));
                return createReturn.Parameters;
            }
        }

        private (CodeConstructor, CodeArgumentReferenceExpression) GenerateExtractionConstructors(
            CodeTypeDeclaration dto, CodeTypeReference innerType)
        {
            // add default constructor
            dto.Members.Add(new CodeConstructor() {Attributes = MemberAttributes.Public});
            var copyConstructor = new CodeConstructor() {Attributes = MemberAttributes.Public};
            dto.Members.Add(copyConstructor);
            var parameter = new CodeParameterDeclarationExpression(innerType, "inner");
            copyConstructor.Parameters.Add(parameter);
            copyConstructor.Parameters.Add(new CodeParameterDeclarationExpression(typeof(IBinaryStore), BinaryStoreParameterName));
            return (copyConstructor, new CodeArgumentReferenceExpression(parameter.Name));
        }

        private (CodeMemberMethod, CodeVariableReferenceExpression) GenerateGetValidationErrors()
        {
            var method = new CodeMemberMethod()
            {
                Name = nameof(ISilaTransferObject.GetValidationErrors),
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                ReturnType = new CodeTypeReference(typeof(string))
            };
            method.WriteDocumentation("Validates the contents of this transfer object", "A validation error or null, if no validation error occurred.");
            var variableDeclaration =
                new CodeVariableDeclarationStatement(typeof(string), "errors",
                    new CodePrimitiveExpression(string.Empty));
            method.Statements.Add(variableDeclaration);
            return (method, new CodeVariableReferenceExpression(variableDeclaration.Name));
        }

        private CodePropertyReferenceExpression GenerateProperty(SiLAElement element, int index,
            CodeTypeDeclaration dto, Action<string, StructureType> structHandler)
        {
            var property = new CodeMemberProperty
            {
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                Name = element.Identifier,
                HasSet = true,
                HasGet = true,
                Type = _translationProvider.GetDtoTypeReference( element.DataType, element.Identifier, structHandler)
            };
            if( element.Identifier == nameof(ISilaRequestObject.CommandIdentifier) )
            {
                property.Name += "_";
            }
            property.WriteDocumentation(element.Description ?? $"The {element.DisplayName} property");
            AddAttribute(property.CustomAttributes, typeof(ProtoMemberAttribute), index);
            dto.Members.Add(property);
            var field = new CodeMemberField
            {
                Name = "_" + property.Name.ToCamelCase(),
                Attributes = MemberAttributes.Private,
                Type = property.Type
            };
            dto.Members.Add(field);
            var fieldRef = new CodeFieldReferenceExpression(null, field.Name);
            property.GetStatements.Add(new CodeMethodReturnStatement(fieldRef));
            property.SetStatements.Add(new CodeAssignStatement(fieldRef,
                new CodePropertySetValueReferenceExpression()));
            return new CodePropertyReferenceExpression(null, property.Name);
        }

        private void ImplementConstraints(Constraints constraints, CodeTypeDeclaration wrapper,
            CodeStatementCollection statements, CodeVariableReferenceExpression resultRef,
            CodePropertyReferenceExpression propertyReference)
        {
            if (constraints.Pattern != null)
            {
                var regexType = new CodeTypeReference(typeof(Regex));
                var patternField = new CodeMemberField
                {
                    Attributes = MemberAttributes.Public | MemberAttributes.Static,
                    Name = propertyReference.PropertyName + "Pattern",
                    Type = regexType,
                    InitExpression = new CodeObjectCreateExpression(regexType,
                        new CodePrimitiveExpression(constraints.Pattern))
                };
                wrapper.Members.Add(patternField);
                var check = new CodeMethodInvokeExpression(
                    new CodeFieldReferenceExpression(null, patternField.Name), nameof(Regex.IsMatch),
                    new CodePropertyReferenceExpression(propertyReference, "Value"));

                statements.Add(CreateValidityStatement(check,
                    $"{propertyReference.PropertyName} '{{0}}' does not match pattern '{constraints.Pattern}'",
                    propertyReference, resultRef));
            }

            if (constraints.MaximalLength != null)
            {
                var maxLength = int.Parse(constraints.MaximalLength);

                var check = new CodeBinaryOperatorExpression(new CodePropertyReferenceExpression(
                        new CodePropertyReferenceExpression(propertyReference, "Value"), "Length"),
                    CodeBinaryOperatorType.LessThanOrEqual,
                    new CodePrimitiveExpression(maxLength));

                statements.Add(CreateValidityStatement(check,
                    $"{propertyReference.PropertyName} '{{0}}' is longer than allowed length {maxLength}.",
                    propertyReference, resultRef));
            }
        }

        private CodeStatement CreateValidityStatement(CodeExpression condition, string format,
            CodePropertyReferenceExpression reference, CodeVariableReferenceExpression resultRef)
        {
            var formatStatement = new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression(typeof(string)),
                "Format",
                new CodePrimitiveExpression(format), new CodePropertyReferenceExpression(reference, "Value"));

            return new CodeConditionStatement(condition,
                new CodeAssignStatement(resultRef,
                    new CodeBinaryOperatorExpression(resultRef, CodeBinaryOperatorType.Add, formatStatement)));
        }

        private void AddAttribute(CodeAttributeDeclarationCollection attributes, Type attributeType,
            params object[] values)
        {
            var declaration = new CodeAttributeDeclaration(new CodeTypeReference(attributeType),
                values.Select(o => new CodeAttributeArgument(o is CodeExpression exp ? exp : new CodePrimitiveExpression(o)))
                      .ToArray());

            attributes.Add(declaration);
        }
    }
}
