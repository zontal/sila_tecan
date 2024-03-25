using Common.Logging;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Tecan.Sila2.Generator.Contracts;
using Tecan.Sila2.Generator.Helper;

#pragma warning disable S3265 // Non-flags enums should not be used in bitwise operations

namespace Tecan.Sila2.Generator.Generators
{
    /// <summary>
    /// Generates interfaces from features
    /// </summary>
    [Export( typeof( IInterfaceGenerator ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class InterfaceGenerator : IInterfaceGenerator
    {
        private IEnumerable<IGeneratorHook> Hooks
        {
            get;
        }

        private readonly ITypeTranslationProvider _translationProvider;
        private readonly ILog _loggingChannel = LogManager.GetLogger<InterfaceGenerator>();
        private readonly IGeneratorConfigSource _configSource;

        [ImportingConstructor]
        public InterfaceGenerator( ITypeTranslationProvider translationProvider, [ImportMany] IEnumerable<IGeneratorHook> hooks, IGeneratorConfigSource configSource )
        {
            Hooks = hooks;
            _translationProvider = translationProvider;
            _configSource = configSource;
        }

        /// <inheritdoc />
        public CodeCompileUnit GenerateInterfaceUnit( Feature feature, string ns )
        {
            _loggingChannel.Info( $"Generating interface for feature {feature.FullyQualifiedIdentifier}" );
            var unit = new CodeCompileUnit();
            var nSpace = new CodeNamespace( ns );
            unit.Namespaces.Add( nSpace );

            var structHelper = new TypeManagement( nSpace, this );

            var interfaceType = GenerateInterface( feature, structHelper );
            nSpace.Types.Add( interfaceType );

            if(feature.Items != null)
            {
                foreach(var dataTypeDefinition in feature.Items.OfType<SiLAElement>())
                {
                    _loggingChannel.Debug( $"Generate domain structure for {dataTypeDefinition.Identifier}" );
                    var type = GenerateDataTypeDefinition( dataTypeDefinition, structHelper.RegisterAnonymousType );
                    if(type != null)
                    {
                        nSpace.Types.Add( type );
                    }
                }

                foreach(var error in feature.Items.OfType<FeatureDefinedExecutionError>())
                {
                    _loggingChannel.Debug( $"Generate exception class for {error.Identifier}" );
                    nSpace.Types.Add( GenerateStandardExecutionError( error ) );
                }

                foreach(var metadata in feature.Items.OfType<FeatureMetadata>())
                {
                    _translationProvider.ExtractType( metadata.DataType, metadata.Identifier, null, structHelper.RegisterStruct );
                }
            }

            if(Hooks != null)
            {
                foreach(var generatorHook in Hooks)
                {
                    _loggingChannel.Debug( $"Executing hook {generatorHook} after generating interface" );
                    generatorHook?.OnInterfaceGenerated( feature, interfaceType, unit );
                }
            }

            structHelper.RegisterAllAnonymousTypes();

            return unit;
        }

        private class TypeManagement : AnonymousTypeHelper
        {
            private readonly CodeNamespace _namespace;
            private readonly InterfaceGenerator _parent;

            public TypeManagement( CodeNamespace ns, InterfaceGenerator parent )
            {
                _namespace = ns;
                _parent = parent;
            }

            public void RegisterType( CodeTypeDeclaration typeDeclaration )
            {
                _namespace.Types.Add( typeDeclaration );
            }

            public void RegisterStruct( string name, StructureType structure )
            {
                var element = new SiLAElement()
                {
                    Identifier = name,
                    DisplayName = name,
                    Description = $"The class {name} reflects an anonymous type from the feature definition.",
                    DataType = new DataTypeType()
                    {
                        Item = structure
                    }
                };
                RegisterType( _parent.GenerateStruct( element, structure, false, RegisterStruct ) );
            }

            public void RegisterAllAnonymousTypes()
            {
                ProcessAll( RegisterStruct );
            }
        }


        private CodeTypeDeclaration GenerateDataTypeDefinition( SiLAElement dataType, Action<string, StructureType> structHandler )
        {
            if(dataType.DataType.Item is ConstrainedType constrained)
            {
                if(constrained.Constraints.Set != null && constrained.Constraints.Set.Length > 0)
                {
                    return GenerateEnumeration( dataType, constrained.Constraints.Set );
                }
            }
            else if(dataType.DataType.Item is StructureType structure)
            {
                return GenerateStruct( dataType, structure, false, structHandler );
            }

            var newStructure = new StructureType
            {
                Element = new[]
                {
                    new SiLAElement
                    {
                        DataType = dataType.DataType,
                        DisplayName = "Value",
                        Identifier = "Value",
                        Description = "The inner value"
                    }
                }
            };
            return GenerateStruct( dataType, newStructure, true, structHandler );
        }

        private CodeTypeDeclaration GenerateStruct( SiLAElement dataType, StructureType structureType, bool inline, Action<string, StructureType> structHandler )
        {
            var name = _translationProvider.ExtractType( new DataTypeType() { Item = dataType.Identifier }, null ).BaseType;
            var summary = dataType.Description ?? $"The {dataType.DisplayName} type";
            var structure = new CodeTypeDeclaration( name )
            {
                Attributes = MemberAttributes.Public,
                TypeAttributes = TypeAttributes.Public,
                IsClass = true
            };
            structure.WriteDocumentation( summary );
            ApplyDataType( dataType, structure );

            var constructor = new CodeConstructor
            {
                Attributes = MemberAttributes.Public,
            };
            structure.Members.Add( constructor );
            var parameterDocs = new Dictionary<string, string>();
            foreach(var element in structureType.Element)
            {
                GenerateStructureElement( element, structure, constructor, inline, structHandler, parameterDocs );
            }

            constructor.WriteDocumentation( "Initializes a new instance", parameters: parameterDocs );

            return structure;
        }

        private void GenerateStructureElement( SiLAElement element, CodeTypeDeclaration structure,
            CodeConstructor constructor, bool inline, Action<string, StructureType> structHandler, IDictionary<string, string> parameterDocs )
        {
            var property = new CodeMemberProperty
            {
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                Name = element.Identifier,
                HasSet = false,
                HasGet = true
            };
            property.WriteDocumentation( element.Description ?? $"The {element.Identifier} property" );
            var targetAttributes = inline ? structure.CustomAttributes : property.CustomAttributes;
            property.Type = _translationProvider.ExtractType( element.DataType,
                structure.Name + element.Identifier, constraints => HandleConstraints( constraints, element.DataType, targetAttributes ), structHandler );
            var parameterName = char.ToLowerInvariant( property.Name[0] ) + property.Name.Substring( 1 );
            var field = new CodeMemberField
            {
                Name = "_" + parameterName,
                Attributes = MemberAttributes.Private,
                Type = property.Type
            };
            structure.Members.Add( property );
            structure.Members.Add( field );
            var fieldReference = new CodeFieldReferenceExpression( null, field.Name );
            property.GetStatements.Add( new CodeMethodReturnStatement( fieldReference ) );
            constructor.Parameters.Add( new CodeParameterDeclarationExpression( property.Type, parameterName ) );
            constructor.Statements.Add( new CodeAssignStatement(
                fieldReference,
                new CodeArgumentReferenceExpression( parameterName ) ) );
        }

        private void HandleConstraints( Constraints constraints,
            DataTypeType type,
            CodeAttributeDeclarationCollection attributes )
        {
            if(constraints.Pattern != null)
            {
                attributes.AddAttribute( typeof( PatternConstraintAttribute ), constraints.Pattern );
            }

            if(constraints.MaximalLength != null)
            {
                var maxLength = int.Parse( constraints.MaximalLength );
                attributes.AddAttribute( typeof( MaximalLengthAttribute ), maxLength );
            }

            HandleUnitConstraint( constraints, attributes );
            HandleThresholdConstraints( constraints, type, attributes );
            HandleContentTypeConstraints( constraints, attributes );
            HandleIdentifierTypeConstraints( constraints, attributes );
            HandleSchemaConstraints( constraints, attributes );
        }

        private static void HandleUnitConstraint( Constraints constraints,
            CodeAttributeDeclarationCollection attributes )
        {
            if(constraints.Unit != null)
            {
                var argList = new List<object>
                {
                    constraints.Unit.Label
                };
                if(constraints.Unit.UnitComponent != null)
                {
                    decimal factor, offset;
                    HandleUnitComponents(constraints, argList, out factor, out offset);

                    if (factor != 1.0m)
                    {
                        argList.Add(new CodeAttributeArgument("Factor", new CodePrimitiveExpression((double)factor)));
                    }

                    if (offset != 0.0m)
                    {
                        argList.Add(new CodeAttributeArgument("Offset", new CodePrimitiveExpression((double)offset)));
                    }
                }

                attributes.AddAttribute( typeof( UnitAttribute ), argList.ToArray() );
            }
        }

        private static void HandleUnitComponents(Constraints constraints, List<object> argList, out decimal factor, out decimal offset)
        {
            factor = constraints.Unit.Factor;
            offset = constraints.Unit.Offset;
            foreach (var unitComponent in constraints.Unit.UnitComponent)
            {
                if (unitComponent.SIUnit != ConstraintsUnitUnitComponentSIUnit.Dimensionless)
                {
                    argList.Add(new CodeAttributeArgument(
                        unitComponent.SIUnit.ToString(),
                        new CodePrimitiveExpression(int.Parse(unitComponent.Exponent))));
                }
            }
        }

        private void HandleThresholdConstraints( Constraints constraints, DataTypeType type,
            CodeAttributeDeclarationCollection attributes )
        {
            if(!IsDateTime( type ))
            {
                if(constraints.MaximalExclusive != null)
                {
                    var maxExclusive = double.Parse( constraints.MaximalExclusive );
                    attributes.AddAttribute( typeof( MaximalExclusiveAttribute ), maxExclusive );
                }

                if(constraints.MaximalInclusive != null)
                {
                    var maxInclusive = double.Parse( constraints.MaximalInclusive );
                    attributes.AddAttribute( typeof( MaximalInclusiveAttribute ), maxInclusive );
                }

                if(constraints.MinimalExclusive != null)
                {
                    var minExclusive = double.Parse( constraints.MinimalExclusive );
                    attributes.AddAttribute( typeof( MinimalExclusiveAttribute ), minExclusive );
                }

                if(constraints.MinimalInclusive != null)
                {
                    var minInclusive = double.Parse( constraints.MinimalInclusive );
                    attributes.AddAttribute( typeof( MinimalInclusiveAttribute ), minInclusive );
                }
            }
            else
            {
                HandleDateTimeThresholds(constraints, attributes);
            }
        }

        private static void HandleDateTimeThresholds(Constraints constraints, CodeAttributeDeclarationCollection attributes)
        {
            if (constraints.MaximalExclusive != null)
            {
                attributes.AddAttribute(typeof(MaximalExclusiveDateAttribute), constraints.MaximalExclusive);
            }
            if (constraints.MaximalInclusive != null)
            {
                attributes.AddAttribute(typeof(MaximalInclusiveDateAttribute), constraints.MaximalInclusive);
            }
            if (constraints.MinimalExclusive != null)
            {
                attributes.AddAttribute(typeof(MinimalExclusiveDateAttribute), constraints.MinimalExclusive);
            }
            if (constraints.MinimalInclusive != null)
            {
                attributes.AddAttribute(typeof(MinimalInclusiveDateAttribute), constraints.MinimalInclusive);
            }
        }

        private CodeExpression CreateDateTimeOffsetExpression( string input )
        {
            if(!DateTimeOffset.TryParse( input, out _ ))
            {
                _loggingChannel.Warn( $"'{input}' could not be parsed as a timestamp. This can lead to runtime errors." );
            }
            return new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression( typeof( DateTimeOffset ) ),
                nameof( DateTimeOffset.Parse ),
                new CodePrimitiveExpression( input ),
                new CodePropertyReferenceExpression( new CodeTypeReferenceExpression( typeof( CultureInfo ) ), nameof( CultureInfo.InvariantCulture ) ) );
        }

        private bool IsDateTime( DataTypeType dataType )
        {
            switch(dataType.Item)
            {
                case BasicType basic:
                    return basic == BasicType.Time || basic == BasicType.Timestamp || basic == BasicType.Date;
                case ConstrainedType constrained:
                    return IsDateTime( constrained.DataType );
                default:
                    return false;
            }
        }

        private void HandleIdentifierTypeConstraints( Constraints constraints,
            CodeAttributeDeclarationCollection attributes )
        {
            if(constraints.FullyQualifiedIdentifierSpecified)
            {
                var identifierTypeReference = new CodeTypeReferenceExpression( typeof( IdentifierType ) );
                var fieldReference = new CodeFieldReferenceExpression();
                fieldReference.TargetObject = identifierTypeReference;
                switch(constraints.FullyQualifiedIdentifier)
                {
                    case ConstraintsFullyQualifiedIdentifier.CommandIdentifier:
                        fieldReference.FieldName = nameof( IdentifierType.CommandIdentifier );
                        break;
                    case ConstraintsFullyQualifiedIdentifier.CommandParameterIdentifier:
                        fieldReference.FieldName = nameof( IdentifierType.CommandParameterIdentifier );
                        break;
                    case ConstraintsFullyQualifiedIdentifier.CommandResponseIdentifier:
                        fieldReference.FieldName = nameof( IdentifierType.CommandResponseIdentifier );
                        break;
                    case ConstraintsFullyQualifiedIdentifier.FeatureIdentifier:
                        fieldReference.FieldName = nameof( IdentifierType.FeatureIdentifier );
                        break;
                    case ConstraintsFullyQualifiedIdentifier.PropertyIdentifier:
                        fieldReference.FieldName = nameof( IdentifierType.PropertyIdentifier );
                        break;
                    case ConstraintsFullyQualifiedIdentifier.DefinedExecutionErrorIdentifier:
                        fieldReference.FieldName = nameof( IdentifierType.DefinedExecutionErrorIdentifier );
                        break;
                    case ConstraintsFullyQualifiedIdentifier.TypeIdentifier:
                        fieldReference.FieldName = nameof( IdentifierType.TypeIdentifier );
                        break;
                    case ConstraintsFullyQualifiedIdentifier.IntermediateCommandResponseIdentifier:
                        fieldReference.FieldName = nameof( IdentifierType.IntermediateResponseIdentifier );
                        break;
                    case ConstraintsFullyQualifiedIdentifier.MetadataIdentifier:
                        fieldReference.FieldName = nameof( IdentifierType.MetadataIdentifier );
                        break;
                    default:
                        throw new ArgumentOutOfRangeException( nameof( constraints.FullyQualifiedIdentifier ) );
                }

                attributes.AddAttribute( typeof( SilaIdentifierTypeAttribute ), fieldReference );
            }
        }

        private void HandleContentTypeConstraints( Constraints constraints,
            CodeAttributeDeclarationCollection attributes )
        {
            if(constraints.ContentType != null)
            {
                var args = new List<object>()
                {
                    constraints.ContentType.Type,
                    constraints.ContentType.Subtype
                };

                if(constraints.ContentType.Parameters != null)
                {
                    args.AddRange( constraints.ContentType.Parameters.Select( p => $"{p.Attribute}={p.Value}" ) );
                }

                attributes.AddAttribute( typeof( ContentTypeAttribute ), args.ToArray() );
            }
        }

        private void HandleSchemaConstraints( Constraints constraints,
            CodeAttributeDeclarationCollection attributes )
        {
            if(constraints.Schema != null)
            {
                var schemaTypeReference = new CodeTypeReferenceExpression( typeof( SchemaType ) );
                var fieldReference = new CodeFieldReferenceExpression();
                fieldReference.TargetObject = schemaTypeReference;

                switch(constraints.Schema.Type)
                {
                    case ConstraintsSchemaType.Json:
                        fieldReference.FieldName = nameof( SchemaType.Json );
                        break;
                    case ConstraintsSchemaType.Xml:
                        fieldReference.FieldName = nameof( SchemaType.Xml );
                        break;
                    default:
                        throw new ArgumentOutOfRangeException( nameof( constraints.Schema ) );
                }

                attributes.AddAttribute( typeof( SchemaAttribute ),
                    constraints.Schema.Item, fieldReference );
            }
        }

        private CodeTypeDeclaration GenerateEnumeration( SiLAElement dataType, string[] literals )
        {
            var enumeration = new CodeTypeDeclaration( dataType.Identifier )
            {
                Attributes = MemberAttributes.Public,
                TypeAttributes = TypeAttributes.Public,
                IsEnum = true
            };
            enumeration.WriteDocumentation( dataType.Identifier ?? "Enumeration consisting of the entries " + string.Join( ", ", literals ) );
            foreach(var literal in literals)
            {
                enumeration.Members.Add( new CodeMemberField
                {
                    Attributes = MemberAttributes.Public | MemberAttributes.Const,
                    Name = literal,
                    Type = new CodeTypeReference( typeof( int ) )
                } );
            }

            ApplyDataType( dataType, enumeration );

            return enumeration;
        }

        private void ApplyDataType( SiLAElement dataType, CodeTypeDeclaration type )
        {
            if(dataType.DisplayName != dataType.Identifier.ToDisplayName())
            {
                type.CustomAttributes.AddAttribute( typeof( SilaDisplayNameAttribute ),
                    dataType.DisplayName );
            }
        }

        private CodeTypeDeclaration GenerateStandardExecutionError( FeatureDefinedExecutionError error )
        {
            var type = new CodeTypeDeclaration( error.Identifier + nameof( Exception ) );
            type.BaseTypes.Add( typeof( Exception ) );
            type.Attributes = MemberAttributes.Public;
            type.TypeAttributes = TypeAttributes.Public;

            if(error.DisplayName != error.Identifier.ToDisplayName())
            {
                type.CustomAttributes.AddAttribute( typeof( SilaDisplayNameAttribute ),
                    error.DisplayName );
            }

            type.WriteDocumentation( error.Description );

            var constructor = new CodeConstructor();
            constructor.Attributes = MemberAttributes.Public;
            constructor.Parameters.Add( new CodeParameterDeclarationExpression( typeof( string ), "message" ) );
            constructor.BaseConstructorArgs.Add( new CodeArgumentReferenceExpression( "message" ) );
            constructor.WriteDocumentation( "Create a new instance", parameters: new Dictionary<string, string>()
            {
                { "message", "The actual error message" }
            } );
            type.Members.Add( constructor );

            return type;
        }

        private string GetTrailingName( string code )
        {
            if(code == null)
            {
                return null;
            }
            var index = code.LastIndexOf( '.' );
            return index > -1 ? code.Substring( index + 1 ) : code;
        }

        private CodeTypeDeclaration GenerateInterface( Feature feature, TypeManagement typeManagement )
        {
            var spec = _configSource.GetFeatureSpec( feature.Identifier );
            var contract = new CodeTypeDeclaration
            {
                Name = GetTrailingName( spec?.Code ) ?? "I" + feature.Identifier,
                Attributes = MemberAttributes.Public,
                IsInterface = true,
                IsPartial = true
            };

            contract.CustomAttributes.AddAttribute( typeof( SilaFeatureAttribute ),
                feature.MaturityLevel == FeatureMaturityLevel.Draft, feature.Category );
            contract.CustomAttributes.AddAttribute( typeof( SilaIdentifierAttribute ),
                feature.Identifier );
            contract.WriteDocumentation( spec?.Description ?? feature.Description );
            if(feature.Identifier != feature.DisplayName)
            {
                contract.CustomAttributes.AddAttribute( typeof( SilaDisplayNameAttribute ),
                    feature.DisplayName );
            }

            if(feature.Items != null)
            {
                foreach(var featureItem in feature.Items)
                {
                    if(featureItem is FeatureCommand command && !CodeGenerationHelper.IsSetterCommand( command, feature.Items.OfType<FeatureProperty>(), out var _ ))
                    {
                        var commandSpec = spec?.Command?.FirstOrDefault( c => string.Equals( command.Identifier, c.Identifier ?? c.Code, StringComparison.OrdinalIgnoreCase ) );
                        _loggingChannel.Debug( $"Generating method for command {command.Identifier}" );
                        contract.Members.Add( GenerateCommandMethod( command, commandSpec, typeManagement ) );
                    }
                    else if(featureItem is FeatureProperty property)
                    {
                        _loggingChannel.Debug( $"Generating property for property {property.Identifier}" );
                        var propertySpec = spec?.Property?.FirstOrDefault( c => string.Equals( property.Identifier, c.Identifier ?? c.Code, StringComparison.OrdinalIgnoreCase ) );
                        contract.Members.Add( GenerateProperty( property, propertySpec, typeManagement.RegisterAnonymousType,
                            feature.Items.OfType<FeatureCommand>().Any( c => CodeGenerationHelper.IsSetterCommand( c, property ) ) ) );
                    }
                }
            }

            return contract;
        }

        private CodeTypeMember GenerateProperty( FeatureProperty property, PropertySpec spec, Action<string, StructureType> structHandler, bool hasSetter )
        {
            CodeTypeMember prop;
            if(spec != null && spec.AsMethodSpecified && spec.AsMethod)
            {
                var method = new CodeMemberMethod
                {
                    Name = spec?.Code ?? property.Identifier,
                    Attributes = MemberAttributes.Public
                };
                method.ReturnType = _translationProvider.ExtractType( property.DataType, property.Identifier, constraints => HandleConstraints( constraints, property.DataType, method.ReturnTypeCustomAttributes ),
                    structHandler );
                prop = method;
            }
            else
            {
                var codeProperty = new CodeMemberProperty
                {
                    Name = spec?.Code ?? property.Identifier,
                    Attributes = MemberAttributes.Public,
                    HasGet = true,
                    HasSet = hasSetter
                };
                codeProperty.Type = _translationProvider.ExtractType( property.DataType, property.Identifier, constraints => HandleConstraints( constraints, property.DataType, codeProperty.CustomAttributes ),
                    structHandler );
                prop = codeProperty;
            }

            AssertNoExpression( spec?.Mapping, property.Identifier );

            if(property.DisplayName != property.Identifier.ToDisplayName())
            {
                prop.CustomAttributes.AddAttribute( typeof( SilaDisplayNameAttribute ),
                    property.DisplayName );
            }

            if(property.Observable == FeaturePropertyObservable.Yes)
            {
                prop.CustomAttributes.AddAttribute( typeof( ObservableAttribute ) );
            }

            if(property.DefinedExecutionErrors != null)
            {
                foreach(var readError in property.DefinedExecutionErrors)
                {
                    prop.CustomAttributes.AddAttribute( typeof( ThrowsAttribute ),
                        new CodeTypeOfExpression( readError + nameof( Exception ) ) );
                }
            }

            prop.WriteDocumentation( spec?.Description ?? property.Description );

            return prop;
        }

        private void AssertNoExpression( TypeMapping expression, string location )
        {
            if(expression != null)
            {
                _loggingChannel.Warn( $"{location} specified a mapping. Mapping configurations are meant for generating server-side code and will be ignored when generating an interface." );
            }
        }

        private CodeTypeMember GenerateCommandMethod( FeatureCommand command, CommandSpec spec, TypeManagement typeManagement )
        {
            var method = new CodeMemberMethod()
            {
                Name = spec?.Code ?? command.Identifier,
                Attributes = MemberAttributes.Public
            };

            if(command.Observable == FeatureCommandObservable.Yes)
            {
                method.CustomAttributes.AddAttribute( typeof( ObservableAttribute ) );
            }

            if(command.DisplayName != command.Identifier.ToDisplayName())
            {
                method.CustomAttributes.AddAttribute( typeof( SilaDisplayNameAttribute ),
                    command.DisplayName );
            }

            var parameters = new Dictionary<string, string>();
            GenerateCommandMethodParameters( command, method, parameters, typeManagement.RegisterAnonymousType );
            GenerateCommandMethodReturnType( command, method, typeManagement.RegisterType, typeManagement.RegisterAnonymousType, out var returnDescription );

            if(command.DefinedExecutionErrors != null)
            {
                foreach(var commandStandardExecutionError in command.DefinedExecutionErrors)
                {
                    method.CustomAttributes.AddAttribute( typeof( ThrowsAttribute ),
                        new CodeTypeOfExpression( commandStandardExecutionError + nameof( Exception ) ) );
                }
            }

            if(command.Observable == FeatureCommandObservable.Yes)
            {
                TurnCommandMethodObservable( command, method, typeManagement.RegisterAnonymousType );
            }

            method.WriteDocumentation( spec?.Description ?? command.Description, returnDescription, parameters );

            return method;
        }

        private void TurnCommandMethodObservable( FeatureCommand command, CodeMemberMethod method, Action<string, StructureType> structHandler )
        {
            var intermediate = command.IntermediateResponse ?? new SiLAElement[0];
            if(command.Response != null && command.Response.Length > 0)
            {
                if(intermediate.Length > 1)
                    throw new NotSupportedException( "More than one intermediate response is not supported" );
                method.ReturnType = intermediate.Length == 1
                    ? new CodeTypeReference( typeof( IIntermediateObservableCommand<,> ).FullName,
                        _translationProvider.ExtractType( intermediate[0].DataType, command.Identifier, null, structHandler ), method.ReturnType )
                    : new CodeTypeReference( typeof( IObservableCommand<> ).FullName, method.ReturnType );
            }
            else
            {
                if(intermediate.Length > 1)
                    throw new NotSupportedException( "More than one intermediate response is not supported" );
                method.ReturnType = intermediate.Length == 1
                    ? new CodeTypeReference( typeof( IIntermediateObservableCommand<> ).FullName,
                        _translationProvider.ExtractType( intermediate[0].DataType, command.Identifier + "Intermediate", null, structHandler ) )
                    : new CodeTypeReference( typeof( IObservableCommand ) );
            }
        }

        private void GenerateCommandMethodReturnType( FeatureCommand command, CodeMemberMethod method, Action<CodeTypeDeclaration> typeAdder,
            Action<string, StructureType> structureHandler, out string documentation )
        {
            if(command.Response != null && command.Response.Length > 0)
            {
                if(command.Response.Length == 1)
                {
                    var response = command.Response[0];
                    method.ReturnTypeCustomAttributes.AddAttribute( typeof( SilaIdentifierAttribute ),
                        response.Identifier );
                    if(response.DisplayName != response.Identifier.ToDisplayName())
                    {
                        method.ReturnTypeCustomAttributes.AddAttribute( typeof( SilaDisplayNameAttribute ),
                            response.DisplayName );
                    }

                    method.ReturnType = _translationProvider.ExtractType( response.DataType, command.Identifier + command.Response[0].Identifier,
                        constraints => HandleConstraints( constraints, response.DataType, method.ReturnTypeCustomAttributes ), structureHandler );
                    documentation = response.Description;
                }
                else
                {
                    var structure = new StructureType() { Element = command.Response };
                    var type = new SiLAElement()
                    {
                        Identifier = command.Identifier + "Response",
                        DisplayName = $"{command.DisplayName} - Response",
                        Description = $"Response type for the {command.DisplayName} command",
                        DataType = new DataTypeType()
                        {
                            Item = structure
                        }
                    };
                    var generatedType = GenerateStruct( type, structure, false, structureHandler );
                    method.ReturnTypeCustomAttributes.Add(
                        new CodeAttributeDeclaration( new CodeTypeReference( typeof( InlineStructAttribute ) ) ) );
                    typeAdder?.Invoke( generatedType );
                    method.ReturnType = new CodeTypeReference( generatedType.Name );
                    documentation = string.Join( ", ", command.Response.Select( r => r.Description ) );
                }
            }
            else
            {
                documentation = null;
            }
        }

        private void GenerateCommandMethodParameters( FeatureCommand command, CodeMemberMethod method, Dictionary<string, string> documentation,
            Action<string, StructureType> structureHandler )
        {
            if(command.Parameter != null)
            {
                foreach(var parameter in command.Parameter)
                {
                    var identifier = char.ToLowerInvariant( parameter.Identifier[0] ) + parameter.Identifier.Substring( 1 );
                    var parameterDeclaration = new CodeParameterDeclarationExpression();
                    parameterDeclaration.Name = identifier;
                    parameterDeclaration.Type = _translationProvider.ExtractType( parameter.DataType,
                        command.Identifier + parameter.Identifier, constraints => HandleConstraints( constraints, parameter.DataType, parameterDeclaration.CustomAttributes ), structureHandler );
                    if(parameter.DisplayName != parameter.Identifier.ToDisplayName())
                    {
                        parameterDeclaration.CustomAttributes.AddAttribute( typeof( SilaDisplayNameAttribute ),
                            parameter.DisplayName );
                    }

                    method.Parameters.Add( parameterDeclaration );
                    documentation.Add( parameterDeclaration.Name, parameter.Description );
                }
            }
        }
    }
}

#pragma warning restore S3265 // Non-flags enums should not be used in bitwise operations