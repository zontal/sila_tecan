using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using Tecan.MAP.Contracts.Services.Logging;
using Tecan.Sila2.Generator.Contracts;
using Tecan.Sila2.Generator.Helper;

namespace Tecan.Sila2.Generator.Generators
{
    /// <summary>
    /// Generates interfaces from features
    /// </summary>
    [Export( typeof(IInterfaceGenerator) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class InterfaceGenerator : IInterfaceGenerator
    {
        private IEnumerable<IGeneratorHook> Hooks
        {
            get;
        }

        private readonly ITypeTranslationProvider _translationProvider;
        private readonly ILoggingChannel _loggingChannel;

        [ImportingConstructor]
        public InterfaceGenerator( ITypeTranslationProvider translationProvider, ILoggingChannelFactory loggingChannelFactory, [ImportMany] IEnumerable<IGeneratorHook> hooks )
        {
            Hooks = hooks;
            _translationProvider = translationProvider;
            _loggingChannel = loggingChannelFactory.CreateChannel( typeof(InterfaceGenerator) );
        }

        /// <inheritdoc />
        public CodeCompileUnit GenerateInterfaceUnit( Feature feature, string ns )
        {
            _loggingChannel.Log($"Generating interface for feature {feature.FullyQualifiedIdentifier}".ToString, Severity.Info);
            var unit = new CodeCompileUnit();
            var nSpace = new CodeNamespace( ns );
            unit.Namespaces.Add( nSpace );

            var structHelper = new TypeManagement(nSpace, this);

            var interfaceType = GenerateInterface( feature, structHelper );
            nSpace.Types.Add( interfaceType );

            foreach( var dataTypeDefinition in feature.Items.OfType<SiLAElement>() )
            {
                _loggingChannel.Log($"Generate domain structure for {dataTypeDefinition.Identifier}".ToString);
                var type = GenerateDataTypeDefinition( dataTypeDefinition, structHelper.RegisterAnonymousType );
                if( type != null )
                {
                    nSpace.Types.Add( type );
                }
            }

            foreach( var error in feature.Items.OfType<FeatureDefinedExecutionError>() )
            {
                _loggingChannel.Log( $"Generate exception class for {error.Identifier}".ToString );
                nSpace.Types.Add( GenerateStandardExecutionError( error ) );
            }

            foreach( var metadata in feature.Items.OfType<FeatureMetadata>() )
            {
                _translationProvider.ExtractType( metadata.DataType, metadata.Identifier, null, structHelper.RegisterStruct );
            }

            if( Hooks != null )
            {
                foreach( var generatorHook in Hooks )
                {
                    _loggingChannel.Log( $"Executing hook {generatorHook} after generating interface".ToString );
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
                    DataType = new DataTypeType()
                    {
                        Item = structure
                    }
                };
                RegisterType( _parent.GenerateStruct( element, structure, false, RegisterStruct ) );
            }

            public void RegisterAllAnonymousTypes()
            {
                ProcessAll(RegisterStruct);
            }
        }


        private CodeTypeDeclaration GenerateDataTypeDefinition( SiLAElement dataType, Action<string, StructureType> structHandler )
        {
            var summary = dataType.Description ?? $"The {dataType.DisplayName} type";
            CodeTypeDeclaration declaration = null;
            if( dataType.DataType.Item is ConstrainedType constrained )
            {
                if( constrained.Constraints.Set != null && constrained.Constraints.Set.Length > 0 )
                {
                    declaration = GenerateEnumeration( dataType, constrained.Constraints.Set );
                }
            }
            else if( dataType.DataType.Item is StructureType structure )
            {
                declaration = GenerateStruct( dataType, structure, false, structHandler );
            }

            if( declaration == null )
            {
                var newStructure = new StructureType
                {
                    Element = new SiLAElement[]
                    {
                        new SiLAElement()
                        {
                            DataType = dataType.DataType,
                            DisplayName = "Value",
                            Identifier = "Value",
                        }
                    }
                };
                declaration = GenerateStruct( dataType, newStructure, true, structHandler );
            }

            declaration.WriteDocumentation( summary );
            return declaration;
        }

        private CodeTypeDeclaration GenerateStruct( SiLAElement dataType, StructureType structureType, bool inline, Action<string, StructureType> structHandler )
        {
            var name = _translationProvider.ExtractType( new DataTypeType() { Item = dataType.Identifier }, null ).BaseType;
            var structure = new CodeTypeDeclaration( name )
            {
                Attributes = MemberAttributes.Public,
                TypeAttributes = TypeAttributes.Public,
                IsStruct = true
            };

            ApplyDataType( dataType, structure );

            var constructor = new CodeConstructor
            {
                Attributes = MemberAttributes.Public,
            };
            structure.Members.Add( constructor );

            foreach( var element in structureType.Element )
            {
                GenerateStructureElement( element, structure, constructor, inline, structHandler );
            }

            return structure;
        }

        private void GenerateStructureElement( SiLAElement element, CodeTypeDeclaration structure,
            CodeConstructor constructor, bool inline, Action<string, StructureType> structHandler )
        {
            var property = new CodeMemberProperty
            {
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                Name = element.Identifier,
                HasSet = false,
                HasGet = true
            };
            property.WriteDocumentation( element.Description );
            var targetAttributes = inline ? structure.CustomAttributes : property.CustomAttributes;
            property.Type = _translationProvider.ExtractType( element.DataType,
                structure.Name + element.Identifier, constraints => HandleConstraints( constraints, targetAttributes ), structHandler );
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
            CodeAttributeDeclarationCollection attributes )
        {
            if( constraints.Pattern != null )
            {
                attributes.AddAttribute( typeof(PatternConstraintAttribute), constraints.Pattern );
            }

            if( constraints.MaximalLength != null )
            {
                var maxLength = int.Parse( constraints.MaximalLength );
                attributes.AddAttribute( typeof(MaximalLengthAttribute), maxLength );
            }

            HandleUnitConstraint( constraints, attributes );
            HandleThresholdConstraints( constraints, attributes );
            HandleContentTypeConstraints( constraints, attributes );
            HandleIdentifierTypeConstraints( constraints, attributes );
            HandleSchemaConstraints( constraints, attributes );
        }

        private static void HandleUnitConstraint( Constraints constraints,
            CodeAttributeDeclarationCollection attributes )
        {
            if( constraints.Unit != null )
            {
                var argList = new List<object>();
                argList.Add( constraints.Unit.Label );
                if( constraints.Unit.UnitComponent != null )
                {
                    var factor = constraints.Unit.Factor;
                    var offset = constraints.Unit.Offset;
                    foreach( var unitComponent in constraints.Unit.UnitComponent )
                    {
                        if( unitComponent.SIUnit != ConstraintsUnitUnitComponentSIUnit.Dimensionless )
                        {
                            argList.Add( new CodeAttributeArgument(
                                unitComponent.SIUnit.ToString(),
                                new CodePrimitiveExpression( int.Parse( unitComponent.Exponent ) ) ) );
                        }
                    }

                    if( factor != 1.0m )
                    {
                        argList.Add( new CodeAttributeArgument( "Factor", new CodePrimitiveExpression( (double)factor ) ) );
                    }

                    if( offset != 0.0m )
                    {
                        argList.Add( new CodeAttributeArgument( "Offset", new CodePrimitiveExpression( (double)offset ) ) );
                    }
                }

                attributes.AddAttribute( typeof(UnitAttribute), argList.ToArray() );
            }
        }

        private void HandleThresholdConstraints( Constraints constraints,
            CodeAttributeDeclarationCollection attributes )
        {
            if( constraints.MaximalExclusive != null )
            {
                var maxExclusive = double.Parse( constraints.MaximalExclusive );
                attributes.AddAttribute( typeof(MaximalExclusiveAttribute), maxExclusive );
            }

            if( constraints.MaximalInclusive != null )
            {
                var maxInclusive = double.Parse( constraints.MaximalInclusive );
                attributes.AddAttribute( typeof(MaximalInclusiveAttribute), maxInclusive );
            }

            if( constraints.MinimalExclusive != null )
            {
                var minExclusive = double.Parse( constraints.MinimalExclusive );
                attributes.AddAttribute( typeof(MinimalExclusiveAttribute), minExclusive );
            }

            if( constraints.MinimalInclusive != null )
            {
                var minInclusive = double.Parse( constraints.MinimalInclusive );
                attributes.AddAttribute( typeof(MinimalInclusiveAttribute), minInclusive );
            }
        }

        private void HandleIdentifierTypeConstraints( Constraints constraints,
            CodeAttributeDeclarationCollection attributes )
        {
            if( constraints.FullyQualifiedIdentifierSpecified )
            {
                var identifierTypeReference = new CodeTypeReferenceExpression( typeof(IdentifierType) );
                var fieldReference = new CodeFieldReferenceExpression();
                fieldReference.TargetObject = identifierTypeReference;
                switch( constraints.FullyQualifiedIdentifier )
                {
                    case ConstraintsFullyQualifiedIdentifier.CommandIdentifier:
                        fieldReference.FieldName = nameof(IdentifierType.CommandIdentifier);
                        break;
                    case ConstraintsFullyQualifiedIdentifier.CommandParameterIdentifier:
                        fieldReference.FieldName = nameof(IdentifierType.CommandParameterIdentifier);
                        break;
                    case ConstraintsFullyQualifiedIdentifier.CommandResponseIdentifier:
                        fieldReference.FieldName = nameof(IdentifierType.CommandResponseIdentifier);
                        break;
                    case ConstraintsFullyQualifiedIdentifier.FeatureIdentifier:
                        fieldReference.FieldName = nameof(IdentifierType.FeatureIdentifier);
                        break;
                    case ConstraintsFullyQualifiedIdentifier.PropertyIdentifier:
                        fieldReference.FieldName = nameof(IdentifierType.PropertyIdentifier);
                        break;
                    case ConstraintsFullyQualifiedIdentifier.DefinedExecutionErrorIdentifier:
                        fieldReference.FieldName = nameof(IdentifierType.DefinedExecutionErrorIdentifier);
                        break;
                    case ConstraintsFullyQualifiedIdentifier.TypeIdentifier:
                        fieldReference.FieldName = nameof(IdentifierType.TypeIdentifier);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException( nameof(constraints.FullyQualifiedIdentifier) );
                }

                attributes.AddAttribute( typeof(SilaIdentifierTypeAttribute), fieldReference );
            }
        }

        private void HandleContentTypeConstraints( Constraints constraints,
            CodeAttributeDeclarationCollection attributes )
        {
            if( constraints.ContentType != null )
            {
                attributes.AddAttribute( typeof(ContentTypeAttribute),
                    constraints.ContentType.Type,
                    constraints.ContentType.Subtype );
            }
        }

        private void HandleSchemaConstraints( Constraints constraints,
            CodeAttributeDeclarationCollection attributes )
        {
            if( constraints.Schema != null )
            {
                var schemaTypeReference = new CodeTypeReferenceExpression( typeof(SchemaType) );
                var fieldReference = new CodeFieldReferenceExpression();
                fieldReference.TargetObject = schemaTypeReference;

                switch( constraints.Schema.Type )
                {
                    case ConstraintsSchemaType.Json:
                        fieldReference.FieldName = nameof(SchemaType.Json);
                        break;
                    case ConstraintsSchemaType.Xml:
                        fieldReference.FieldName = nameof(SchemaType.Xml);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException( nameof(constraints.Schema) );
                }

                attributes.AddAttribute( typeof(SchemaAttribute),
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
            foreach( var literal in literals )
            {
                enumeration.Members.Add( new CodeMemberField
                {
                    Attributes = MemberAttributes.Public | MemberAttributes.Const,
                    Name = literal,
                    Type = new CodeTypeReference( typeof(int) )
                } );
            }

            ApplyDataType( dataType, enumeration );

            return enumeration;
        }

        private void ApplyDataType( SiLAElement dataType, CodeTypeDeclaration type )
        {
            if( dataType.DisplayName != dataType.Identifier.ToDisplayName() )
            {
                type.CustomAttributes.AddAttribute( typeof(SilaDisplayNameAttribute),
                    dataType.DisplayName );
            }
        }

        private CodeTypeDeclaration GenerateStandardExecutionError( FeatureDefinedExecutionError error )
        {
            var type = new CodeTypeDeclaration( error.Identifier + nameof(Exception) );
            type.BaseTypes.Add( typeof(Exception) );
            type.Attributes = MemberAttributes.Public;
            type.TypeAttributes = TypeAttributes.Public;

            if( error.DisplayName != error.Identifier.ToDisplayName() )
            {
                type.CustomAttributes.AddAttribute( typeof(SilaDisplayNameAttribute),
                    error.DisplayName );
            }

            type.WriteDocumentation( error.Description );

            var constructor = new CodeConstructor();
            constructor.Attributes = MemberAttributes.Public;
            constructor.Parameters.Add( new CodeParameterDeclarationExpression( typeof(string), "message" ) );
            constructor.BaseConstructorArgs.Add( new CodeArgumentReferenceExpression( "message" ) );
            constructor.WriteDocumentation( "Create a new instance", parameters: new Dictionary<string, string>()
            {
                { "message", "The actual error message" }
            } );
            type.Members.Add( constructor );

            return type;
        }

        private CodeTypeDeclaration GenerateInterface( Feature feature, TypeManagement typeManagement )
        {
            var contract = new CodeTypeDeclaration
            {
                Name = "I" + feature.Identifier,
                Attributes = MemberAttributes.Public,
                IsInterface = true,
                IsPartial = true
            };

            contract.CustomAttributes.AddAttribute( typeof(SilaFeatureAttribute),
                feature.MaturityLevel == FeatureMaturityLevel.Draft, feature.Category );
            contract.CustomAttributes.AddAttribute( typeof(SilaIdentifierAttribute),
                feature.Identifier );
            contract.WriteDocumentation( feature.Description );
            if( feature.Identifier != feature.DisplayName )
            {
                contract.CustomAttributes.AddAttribute( typeof(SilaDisplayNameAttribute),
                    feature.DisplayName );
            }

            foreach( var featureItem in feature.Items )
            {
                if( featureItem is FeatureCommand command && !CodeGenerationHelper.IsSetterCommand( command, feature.Items.OfType<FeatureProperty>()) )
                {
                    _loggingChannel.Log($"Generating method for command {command.Identifier}".ToString);
                    contract.Members.Add( GenerateCommandMethod( command, typeManagement ) );
                }
                else if( featureItem is FeatureProperty property )
                {
                    _loggingChannel.Log( $"Generating property for property {property.Identifier}".ToString );
                    contract.Members.Add( GenerateProperty( property, typeManagement.RegisterAnonymousType,
                        feature.Items.OfType<FeatureCommand>().Any( c => CodeGenerationHelper.IsSetterCommand( c, property ) ) ) );
                }
            }

            return contract;
        }

        private CodeTypeMember GenerateProperty( FeatureProperty property, Action<string, StructureType> structHandler, bool hasSetter )
        {
            var prop = new CodeMemberProperty
            {
                Name = property.Identifier,
                Attributes = MemberAttributes.Public,
                HasGet = true,
                HasSet = hasSetter
            };

            if( property.DisplayName != property.Identifier.ToDisplayName() )
            {
                prop.CustomAttributes.AddAttribute( typeof(SilaDisplayNameAttribute),
                    property.DisplayName );
            }

            if( property.Observable == FeaturePropertyObservable.Yes )
            {
                prop.CustomAttributes.AddAttribute( typeof(ObservableAttribute) );
            }

            prop.Type = _translationProvider.ExtractType( property.DataType, property.Identifier, constraints => HandleConstraints( constraints, prop.CustomAttributes ),
                structHandler );

            if( property.DefinedExecutionErrors != null )
            {
                foreach( var readError in property.DefinedExecutionErrors )
                {
                    prop.CustomAttributes.AddAttribute( typeof(ThrowsAttribute),
                        new CodeTypeOfExpression( readError + nameof(Exception) ) );
                }
            }

            prop.WriteDocumentation( property.Description );

            return prop;
        }

        private CodeTypeMember GenerateCommandMethod( FeatureCommand command, TypeManagement typeManagement )
        {
            var method = new CodeMemberMethod()
            {
                Name = command.Identifier,
                Attributes = MemberAttributes.Public
            };

            if( command.Observable == FeatureCommandObservable.Yes )
            {
                method.CustomAttributes.AddAttribute(typeof(ObservableAttribute));
            }

            if( command.DisplayName != command.Identifier.ToDisplayName() )
            {
                method.CustomAttributes.AddAttribute( typeof(SilaDisplayNameAttribute),
                    command.DisplayName );
            }

            var parameters = new Dictionary<string, string>();
            GenerateCommandMethodParameters( command, method, parameters, typeManagement.RegisterAnonymousType );
            GenerateCommandMethodReturnType( command, method, typeManagement.RegisterType, typeManagement.RegisterAnonymousType, out var returnDescription );

            if( command.DefinedExecutionErrors != null )
            {
                foreach( var commandStandardExecutionError in command.DefinedExecutionErrors )
                {
                    method.CustomAttributes.AddAttribute( typeof(ThrowsAttribute),
                        new CodeTypeOfExpression( commandStandardExecutionError + nameof(Exception) ) );
                }
            }

            if( command.Observable == FeatureCommandObservable.Yes )
            {
                TurnCommandMethodObservable( command, method, typeManagement.RegisterAnonymousType );
            }

            method.WriteDocumentation( command.Description, returnDescription, parameters );

            return method;
        }

        private void TurnCommandMethodObservable( FeatureCommand command, CodeMemberMethod method, Action<string, StructureType> structHandler )
        {
            var intermediate = command.IntermediateResponse ?? new SiLAElement[0];
            if( command.Response != null && command.Response.Length > 0 )
            {
                if( intermediate.Length > 1 )
                    throw new NotSupportedException( "More than one intermediate response is not supported" );
                method.ReturnType = intermediate.Length == 1
                    ? new CodeTypeReference( typeof(IIntermediateObservableCommand<,>).FullName,
                        _translationProvider.ExtractType( intermediate[0].DataType, command.Identifier, null, structHandler ), method.ReturnType )
                    : new CodeTypeReference( typeof(IObservableCommand<>).FullName, method.ReturnType );
            }
            else
            {
                if( intermediate.Length > 1 )
                    throw new NotSupportedException( "More than one intermediate response is not supported" );
                method.ReturnType = intermediate.Length == 1
                    ? new CodeTypeReference( typeof(IIntermediateObservableCommand<>).FullName,
                        _translationProvider.ExtractType( intermediate[0].DataType, command.Identifier + "Intermediate", null, structHandler ) )
                    : new CodeTypeReference( typeof(IObservableCommand) );
            }
        }

        private void GenerateCommandMethodReturnType( FeatureCommand command, CodeMemberMethod method, Action<CodeTypeDeclaration> typeAdder,
            Action<string, StructureType> structureHandler, out string documentation )
        {
            if( command.Response != null && command.Response.Length > 0 )
            {
                if( command.Response.Length == 1 )
                {
                    var response = command.Response[0];
                    method.ReturnTypeCustomAttributes.AddAttribute( typeof(SilaIdentifierAttribute),
                        response.Identifier );
                    if( response.DisplayName != response.Identifier.ToDisplayName() )
                    {
                        method.ReturnTypeCustomAttributes.AddAttribute( typeof(SilaDisplayNameAttribute),
                            response.DisplayName );
                    }

                    method.ReturnType = _translationProvider.ExtractType( response.DataType, command.Identifier + command.Response[0].Identifier,
                        constraints => HandleConstraints( constraints, method.ReturnTypeCustomAttributes ), structureHandler );
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
                        new CodeAttributeDeclaration( new CodeTypeReference( typeof(InlineStructAttribute) ) ) );
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
            if( command.Parameter != null )
            {
                foreach( var parameter in command.Parameter )
                {
                    var identifier = char.ToLowerInvariant( parameter.Identifier[0] ) + parameter.Identifier.Substring( 1 );
                    var parameterDeclaration = new CodeParameterDeclarationExpression();
                    parameterDeclaration.Name = identifier;
                    parameterDeclaration.Type = _translationProvider.ExtractType( parameter.DataType,
                        command.Identifier + parameter.Identifier, constraints => HandleConstraints( constraints, parameterDeclaration.CustomAttributes ), structureHandler );
                    if( parameter.DisplayName != parameter.Identifier.ToDisplayName() )
                    {
                        parameterDeclaration.CustomAttributes.AddAttribute( typeof(SilaDisplayNameAttribute),
                            parameter.DisplayName );
                    }

                    method.Parameters.Add( parameterDeclaration );
                    documentation.Add( parameterDeclaration.Name, parameter.Description );
                }
            }
        }
    }
}