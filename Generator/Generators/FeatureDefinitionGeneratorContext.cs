using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Tecan.MAP.Contracts.Services.Logging;
using Tecan.Sila2.Generator.Contracts;
using Tecan.Sila2.Generator.Helper;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.Generator.Generators
{
    internal class FeatureDefinitionGeneratorContext
    {

        private readonly ICodeNameRegistry _nameRegistry;
        private readonly Dictionary<string, Dictionary<string, XElement>> _loadedDocumentation = new Dictionary<string, Dictionary<string, XElement>>();
        private readonly Func<string, Constraints> _constraintResolver;
        private readonly ITypeTranslationProvider _translationProvider;
        private readonly ILoggingChannel _loggingChannel;
        private readonly IAmbiguityResolver _ambiguityResolver;
        private readonly PredefinedFeatureGenerationConfigModel _predefinedFeatureGenerationConfigModel;

        private static readonly string[] ArgumentExceptionTypeNames =
        {
            typeof(ArgumentException).FullName,
            typeof(ArgumentNullException).FullName,
            typeof(ArgumentOutOfRangeException).FullName
        };

        public FeatureDefinitionGeneratorContext( ILoggingChannel loggingChannel, ICodeNameRegistry nameRegistry, Func<string, Constraints> constraintResolver, 
            ITypeTranslationProvider translationProvider, IAmbiguityResolver ambiguityResolver, 
            PredefinedFeatureGenerationConfigModel predefinedFeatureGenerationConfigModel)
        {
            _nameRegistry = nameRegistry;
            _constraintResolver = constraintResolver;
            _translationProvider = translationProvider;
            _loggingChannel = loggingChannel;
            _ambiguityResolver = ambiguityResolver;
            _predefinedFeatureGenerationConfigModel = predefinedFeatureGenerationConfigModel;
        }

        public object[] GenerateFeature(Type interfaceType, ICollection<string> namespaceCollector )
        {
            var items = new List<object>();
            var executeExceptions = new HashSet<Type>();
            var dataTypes = new HashSet<Type>();

            AddInterfaceMembers( interfaceType, items, executeExceptions, dataTypes );
            namespaceCollector?.Add( interfaceType.Namespace );

            GenerateExceptions( executeExceptions, items, namespaceCollector );
            GenerateDataTypes( dataTypes, items, namespaceCollector );

            return items.ToArray();
        }

        public void TryLoadDocumentation( Assembly assembly )
        {
            var documentationPath = Path.ChangeExtension( assembly.Location, ".xml" );
            if(File.Exists( documentationPath ) && !_loadedDocumentation.ContainsKey( assembly.FullName ))
            {
                try
                {
                    var docs = new Dictionary<string, XElement>();
                    var doc = XDocument.Load( documentationPath );
                    foreach(var element in doc.Root.Element( XName.Get( "members" ) ).Elements())
                    {
                        var reference = element.Attribute( XName.Get( "name" ) ).Value;
                        docs.Add( reference, element );
                    }

                    _loadedDocumentation.Add( assembly.FullName, docs );
                    _loggingChannel.Log($"Documentation for {assembly.FullName} successfully loaded.".ToString, Severity.Info);
                }
                catch(Exception e)
                {
                    _loggingChannel.Log( $"Loading documentation for assembly {assembly.FullName} failed: {e.Message}.".ToString, Severity.Warning );
                }
            }
        }

        private void GenerateDataTypes( HashSet<Type> dataTypes, List<object> items,
            ICollection<string> namespaceCollector )
        {
            var processQueue = new Queue<Type>(dataTypes);
            while( processQueue.Any() && processQueue.Dequeue() is var dataType && dataType != null )
            {
                items.Add( GenerateDataType( dataType, t =>
                {
                    if( dataTypes.Add( t ) )
                    {
                        processQueue.Enqueue( t );
                    }
                } ) );
                namespaceCollector?.Add( dataType.Namespace );
            }
        }

        private void GenerateExceptions( ICollection<Type> exceptions, ICollection<object> items,
            ICollection<string> namespaceCollector )
        {
            foreach(var executeException in exceptions)
            {
                items.Add( GenerateExecuteException( executeException ) );
                namespaceCollector?.Add( executeException.Namespace );
            }
        }

        private void AddInterfaceMembers( Type interfaceType, ICollection<object> items,
            ICollection<Type> definedExceptions, ICollection<Type> dataTypes )
        {
            foreach(var methodInfo in interfaceType.GetMethods())
            {
                if(!methodInfo.IsSpecialName)
                {
                    items.Add( GenerateCommand( methodInfo, definedExceptions, dataTypes ) );
                }
            }

            foreach(var propertyInfo in interfaceType.GetProperties())
            {
                if(!typeof( IRequestInterceptor ).IsAssignableFrom( propertyInfo.PropertyType ))
                {
                    items.Add( GenerateProperty( propertyInfo, definedExceptions, dataTypes, out var setterCommand ) );
                    if( setterCommand != null )
                    {
                        items.Add( setterCommand );
                    }
                }
                else
                {
                    items.Add( GenerateMetadata( propertyInfo, definedExceptions, dataTypes ) );
                }
            }
        }

        private FeatureMetadata GenerateMetadata( PropertyInfo propertyInfo, ICollection<Type> definedExceptions, ICollection<Type> dataTypes )
        {
            _loggingChannel.Log( $"Generate SiLA2 metadata for property {propertyInfo.Name}".ToString );
            var (identifier, displayName, description) = ParseElementData( propertyInfo );
            var exceptionIdentifiers = ParseExceptionNamesAndRegister( propertyInfo, definedExceptions );

            var metadataType = propertyInfo.GetCustomAttribute<MetadataTypeAttribute>();
            if(metadataType == null)
            {
                throw new InvalidOperationException( $"The property {propertyInfo.Name} misses the required MetadataType attribute." );
            }

            var metadata = new FeatureMetadata
            {
                Identifier = identifier,
                DisplayName = displayName,
                Description = description,
                DefinedExecutionErrors = exceptionIdentifiers.Any() ? exceptionIdentifiers.ToArray() : null,
                DataType = GenerateTypeReference( metadataType.MetadataType, identifier, new MemberAttributeReader( propertyInfo ), true,
                    dataTypes.Add )
            };

            return metadata;
        }

        private SiLAElement GenerateDataType( Type dataType, Action<Type> typeAction )
        {
            var (id, name, description) = ParseElementData( dataType );

            return new SiLAElement()
            {
                Identifier = id,
                DisplayName = name,
                Description = description,
                DataType = GenerateTypeReference( dataType, dataType.FullName, new MemberAttributeReader( dataType ), false, typeAction )
            };
        }

        private FeatureDefinedExecutionError GenerateExecuteException( Type exception )
        {
            var (identifier, displayName, description) = ParseElementData( exception, suffix: nameof( Exception ) );

            return new FeatureDefinedExecutionError
            {
                Identifier = identifier,
                Description = description,
                DisplayName = displayName
            };
        }

        private FeatureProperty GenerateProperty( PropertyInfo propertyInfo, ICollection<Type> readExceptions,
            ICollection<Type> dataTypes, out FeatureCommand setterCommand )
        {
            _loggingChannel.Log( $"Generate SiLA2 property for {propertyInfo.Name}".ToString );
            var (identifier, displayName, description) = ParseElementData( propertyInfo );

            var definedErrors = ParseExceptionNamesAndRegister( propertyInfo, readExceptions );
            var isDynamic = propertyInfo.GetCustomAttribute<ObservableAttribute>() != null;
            setterCommand = null;
            var property = new FeatureProperty
            {
                Identifier = identifier,
                DisplayName = displayName,
                Description = description,
                DataType = GenerateTypeReference( propertyInfo.PropertyType, identifier, new MemberAttributeReader( propertyInfo ), true,
                    dataTypes.Add ),
                DefinedExecutionErrors = definedErrors.Any() ? definedErrors.ToArray() : null,
                Observable = isDynamic ? FeaturePropertyObservable.Yes : FeaturePropertyObservable.No
            };
            if( propertyInfo.CanWrite )
            {
                _loggingChannel.Log($"Property {propertyInfo.Name} is writable, generating setter command.".ToString);
                setterCommand = new FeatureCommand()
                {
                    Identifier = "Set" + identifier,
                    DefinedExecutionErrors = property.DefinedExecutionErrors,
                    Description = property.Description,
                    DisplayName = "Set " + displayName,
                    Observable = FeatureCommandObservable.No,
                    Parameter = new SiLAElement[]
                    {
                        new SiLAElement()
                        {
                            Identifier = "Value",
                            DisplayName = "Value",
                            Description = "The new " + property.DisplayName,
                            DataType = property.DataType
                        }
                    }
                };
            }
            return property;
        }

        private HashSet<string> ParseExceptionNamesAndRegister( MemberInfo member, ICollection<Type> exceptions )
        {
            var errors = new HashSet<string>();
            foreach(var throwAtt in member.GetCustomAttributes<ThrowsAttribute>())
            {
                AddError( exceptions, throwAtt.ExceptionType, errors );
            }

            var docEntry = GetDocEntry( member );
            if(docEntry != null)
            {
                _loggingChannel.Log($"Found documentation for {member.Name}, extracting exceptions.".ToString);
                var assembly = member.DeclaringType.Assembly;

                foreach(var exceptionElement in docEntry.Elements( XName.Get( "exception" ) ))
                {
                    var typeName = exceptionElement.Attribute( XName.Get( "cref" ) )?.Value?.Substring( 2 );
                    if(typeName != null)
                    {
                        if( ArgumentExceptionTypeNames.Contains( typeName ) || typeof(Exception).FullName.Equals(typeName))
                        {
                            _loggingChannel.Log($"Ignoring exception {typeName} because it is covered by SiLA2 validation errors.".ToString);
                            continue;
                        }
                        var exceptionType = assembly.GetType( typeName, false );
                        if (exceptionType == null)
                        {
                            var assemblyNames = assembly.GetReferencedAssemblies();
                            foreach (var assemblyName in assemblyNames)
                            {
                                Assembly referenceAssembly = Assembly.Load(assemblyName.FullName);
                                exceptionType = referenceAssembly.GetType(typeName, false);
                                if (exceptionType != null)
                                {
                                    break;
                                }
                            }
                        }
                        if (exceptionType != null)
                        {
                            _loggingChannel.Log($"Found exception type {exceptionType.Name}".ToString);
                            AddError( exceptions, exceptionType, errors );
                            continue;
                        }
                    }
                    _loggingChannel.Log($"Exception {typeName} could not be found.".ToString, Severity.Warning);
                }
            }

            return errors;
        }

        private void AddError( ICollection<Type> exceptions, Type exceptionType, ICollection<string> errors )
        {
            exceptions.Add( exceptionType );
            var (identifier, _, _) = ParseElementData( exceptionType, suffix: nameof( Exception ) );
            errors.Add( identifier );
        }

        private DataTypeType GenerateTypeReference( Type type, string origin, IAttributeReader reader, bool allowReference,
            Action<Type> typeAction )
        {
            if(!_translationProvider.TryTranslate(type, origin, out var resultType))
            {
                throw new NotSupportedException();
            }

            if( typeAction != null )
            {
                _translationProvider.TraverseTypes(type, origin, typeAction);
            }

            if( !allowReference && resultType.Item is string )
            {
                if(type.IsEnum)
                {
                    return GenerateEnum( type );
                }

                return GenerateStructure( type, typeAction );
            }

            if(reader != null)
            {
                resultType = AsConstrained( reader, resultType, _constraintResolver?.Invoke( origin ) );
            }

            return resultType;
        }

        private static DataTypeType AsConstrained( IAttributeReader member, DataTypeType resultType, Constraints constraints )
        {
            var requiresConstraints = constraints != null;
            constraints ??= new Constraints();

            requiresConstraints |= ParseStringConstraints( member, constraints );
            requiresConstraints |= ParseThresholds( member, constraints );
            requiresConstraints |= ParseUnits( member, constraints );
            requiresConstraints |= ParseContentType( member, constraints );
            requiresConstraints |= ParseSchemaConstraints( member, constraints );
            requiresConstraints |= ParseIdentifierType( member, constraints );

            if(requiresConstraints)
            {
                var constrainedType = new ConstrainedType
                {
                    DataType = resultType,
                    Constraints = constraints
                };
                resultType = new DataTypeType { Item = constrainedType };
            }

            return resultType;
        }

        private static bool ParseUnits( IAttributeReader member, Constraints constraints )
        {
            var requiresConstraints = false;
            if(member.GetCustomAttribute<UnitAttribute>() is var unit && unit != null)
            {
                var u = new ConstraintsUnit
                {
                    Label = unit.Label,
                    Offset = (decimal)unit.Offset,
                    Factor = (decimal)unit.Factor
                };
                var components = new List<ConstraintsUnitUnitComponent>();

                AddComponent( ConstraintsUnitUnitComponentSIUnit.Meter, unit.Meter, components );
                AddComponent( ConstraintsUnitUnitComponentSIUnit.Second, unit.Second, components );
                AddComponent( ConstraintsUnitUnitComponentSIUnit.Kilogram, unit.Kilogram, components );
                AddComponent( ConstraintsUnitUnitComponentSIUnit.Ampere, unit.Ampere, components );
                AddComponent( ConstraintsUnitUnitComponentSIUnit.Kelvin, unit.Kelvin, components );
                AddComponent( ConstraintsUnitUnitComponentSIUnit.Mole, unit.Mole, components );
                AddComponent( ConstraintsUnitUnitComponentSIUnit.Candela, unit.Candela, components );
                u.UnitComponent = components.ToArray();
                constraints.Unit = u;
                requiresConstraints = true;
            }

            return requiresConstraints;
        }

        private static bool ParseThresholds( IAttributeReader member, Constraints constraints )
        {
            var requiresConstraints = false;
            if(member.GetCustomAttribute<MinimalExclusiveAttribute>() is var minimal && minimal != null)
            {
                constraints.MinimalExclusive = minimal.Threshold.ToString( "R" );
                requiresConstraints = true;
            }

            if(member.GetCustomAttribute<MaximalInclusiveAttribute>() is var maximal && maximal != null)
            {
                constraints.MaximalInclusive = maximal.Threshold.ToString( "R" );
                requiresConstraints = true;
            }

            if(member.GetCustomAttribute<MinimalInclusiveAttribute>() is var minimalInclusive &&
                minimalInclusive != null)
            {
                constraints.MinimalInclusive = minimalInclusive.Threshold.ToString( "R" );
                requiresConstraints = true;
            }

            if(member.GetCustomAttribute<MaximalInclusiveAttribute>() is var maximalInclusive &&
                maximalInclusive != null)
            {
                constraints.MaximalInclusive = maximalInclusive.Threshold.ToString( "R" );
                requiresConstraints = true;
            }

            return requiresConstraints;
        }

        private static bool ParseStringConstraints( IAttributeReader member, Constraints constraints )
        {
            var requiresConstraints = false;
            if(member.GetCustomAttribute<PatternConstraintAttribute>() is var pattern && pattern != null)
            {
                constraints.Pattern = pattern.Pattern;
                requiresConstraints = true;
            }

            if(member.GetCustomAttribute<MaximalLengthAttribute>() is var maximalLength && maximalLength != null)
            {
                constraints.MaximalLength = maximalLength.MaxLength.ToString();
                requiresConstraints = true;
            }

            return requiresConstraints;
        }

        private static bool ParseSchemaConstraints( IAttributeReader member, Constraints constraints )
        {
            var requiresConstraints = false;
            if(member.GetCustomAttribute<SchemaAttribute>() is var schema && schema != null)
            {
                constraints.Schema = new ConstraintsSchema()
                {
                    Item = schema.Schema,
                    ItemElementName = Uri.TryCreate( schema.Schema, UriKind.Absolute, out var _ )
                        ? ItemChoiceType.Url
                        : ItemChoiceType.Inline,
                    Type = ConvertSchemaType( schema.Type )
                };
                requiresConstraints = true;
            }

            return requiresConstraints;
        }

        private static ConstraintsSchemaType ConvertSchemaType( SchemaType schemaType )
        {
            switch(schemaType)
            {
                case SchemaType.Xml:
                    return ConstraintsSchemaType.Xml;
                case SchemaType.Json:
                    return ConstraintsSchemaType.Json;
                default:
                    throw new ArgumentOutOfRangeException( nameof( schemaType ) );
            }
        }

        private static bool ParseContentType( IAttributeReader member, Constraints constraints )
        {
            var requiresConstraints = false;
            if(member.GetCustomAttribute<ContentTypeAttribute>() is var contentType && contentType != null)
            {
                constraints.ContentType = new ConstraintsContentType()
                {
                    Type = contentType.Type,
                    Subtype = contentType.SubType
                };
                requiresConstraints = true;
            }

            return requiresConstraints;
        }

        private static bool ParseIdentifierType( IAttributeReader member, Constraints constraints )
        {
            var requiresConstraints = false;
            if(member.GetCustomAttribute<SilaIdentifierTypeAttribute>() is var identifierType && identifierType != null)
            {
                constraints.FullyQualifiedIdentifierSpecified = true;
                switch(identifierType.Type)
                {
                    case IdentifierType.CommandIdentifier:
                        constraints.FullyQualifiedIdentifier = ConstraintsFullyQualifiedIdentifier.CommandIdentifier;
                        break;
                    case IdentifierType.CommandParameterIdentifier:
                        constraints.FullyQualifiedIdentifier =
                            ConstraintsFullyQualifiedIdentifier.CommandParameterIdentifier;
                        break;
                    case IdentifierType.CommandResponseIdentifier:
                        constraints.FullyQualifiedIdentifier =
                            ConstraintsFullyQualifiedIdentifier.CommandResponseIdentifier;
                        break;
                    case IdentifierType.DefinedExecutionErrorIdentifier:
                        constraints.FullyQualifiedIdentifier =
                            ConstraintsFullyQualifiedIdentifier.DefinedExecutionErrorIdentifier;
                        break;
                    case IdentifierType.FeatureIdentifier:
                        constraints.FullyQualifiedIdentifier = ConstraintsFullyQualifiedIdentifier.FeatureIdentifier;
                        break;
                    case IdentifierType.PropertyIdentifier:
                        constraints.FullyQualifiedIdentifier = ConstraintsFullyQualifiedIdentifier.PropertyIdentifier;
                        break;
                    case IdentifierType.TypeIdentifier:
                        constraints.FullyQualifiedIdentifier = ConstraintsFullyQualifiedIdentifier.TypeIdentifier;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException( nameof( identifierType ) );
                }
                requiresConstraints = true;
            }

            return requiresConstraints;
        }

        private static void AddComponent( ConstraintsUnitUnitComponentSIUnit unit, sbyte exponent,
            ICollection<ConstraintsUnitUnitComponent> components )
        {
            if(exponent != 0)
            {
                components.Add( new ConstraintsUnitUnitComponent()
                {
                    Exponent = exponent.ToString(),
                    SIUnit = unit
                } );
            }
        }

        private DataTypeType GenerateEnum( Type type )
        {
            var fields = Enum.GetNames( type );
            return new DataTypeType
            {
                Item = new ConstrainedType
                {
                    DataType = new DataTypeType { Item = BasicType.String },
                    Constraints = new Constraints
                    {
                        Set = fields
                    }
                }
            };
        }

        private DataTypeType GenerateStructure( Type type, Action<Type> typeFoundAction )
        {
            var identifier = type.GetCustomAttribute<SilaIdentifierAttribute>()?.Identifier ?? type.Name;
            var properties = type.GetProperties();
            if(properties.Length == 1 && properties[0].Name == "Value")
            {
                var prop = properties[0];
                return GenerateTypeReference( prop.PropertyType, identifier + "." + prop.Name, new MemberAttributeReader( type ), true, null );
            }

            return new DataTypeType
            {
                Item = new StructureType
                {
                    Element = GenerateStructureElements( type, typeFoundAction, identifier )
                }
            };
        }

        private SiLAElement[] GenerateStructureElements( Type type, Action<Type> typeFoundAction, string identifier )
        {
            return (from prop in DetermineStructureProperties( type )
                    select new SiLAElement
                    {
                        DataType = GenerateTypeReference( (prop as PropertyInfo)?.PropertyType ?? (prop as FieldInfo)?.FieldType  ?? (prop as MethodInfo)?.ReturnType , identifier + "." + prop.Name, new MemberAttributeReader( prop ),
                            true, typeFoundAction ),
                        Identifier = prop.GetCustomAttribute<SilaIdentifierAttribute>()?.Identifier ?? prop.Name,
                        DisplayName = prop.GetCustomAttribute<SilaDisplayNameAttribute>()?.DisplayName ??
                                      prop.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ??
                                      prop.Name,
                        Description = prop.GetCustomAttribute<SilaDescriptionAttribute>()?.Description ??
                                      prop.GetCustomAttribute<DescriptionAttribute>()?.Description ??
                                      GetDocumentationSummary(prop) ?? string.Empty
                    }).ToArray();
        }

        private MemberInfo GetMemberInfo(MemberInfo[] memberInfos, string parameterName)
        {
            if (memberInfos.FirstOrDefault(x => x.Name.StartsWith(parameterName, StringComparison.OrdinalIgnoreCase)) is var member && member != null)
            {
                return member;
            }
            if (memberInfos.Length == 1)
            {
                return memberInfos[0];
            }

            return null;
        }

        private MemberInfo GetMemberInfo(Type type, string parameterName)
        {
            if (type.GetProperty(parameterName.ToPascalCase()) is var property && property!= null)
            {
                return property;
            }
            if (type.GetField(parameterName.ToPascalCase()) is var field && field != null)
            {
                return field;
            }
            if(GetMemberInfo(type.GetProperties(), parameterName) is var memberProperty && memberProperty!= null)
            {
                return memberProperty;
            }
            if (GetMemberInfo(type.GetFields(), parameterName) is var memberField && memberField != null)
            {
                return memberField;
            }

            string id = $"{type.Name}.{parameterName}";
            if (_predefinedFeatureGenerationConfigModel?.PredefinedAmbiguousProperties!=null && _predefinedFeatureGenerationConfigModel.PredefinedAmbiguousProperties.ContainsKey(id))
            {
                return GetMemberInfo(type, _predefinedFeatureGenerationConfigModel.PredefinedAmbiguousProperties[id]);
            }

            List<MemberInfo> allowedProperties = new List<MemberInfo>(type.GetProperties());
            allowedProperties.AddRange(type.GetFields());
            var questionforUser = $"Property {id} does not matched. Enter property name for this from allowed options:";
            if (_ambiguityResolver?.GetUserDefinedProperty(questionforUser, allowedProperties) is var userDefinedProperty && !string.IsNullOrEmpty(userDefinedProperty))
            {
                return GetMemberInfo(type, userDefinedProperty);
            }
            throw new InvalidOperationException(
                                  $"Could not find matching property for constructor parameter {parameterName} of type {type.Name}");
        }

        private IEnumerable<MemberInfo> DetermineStructureProperties( Type type )
        {
            var constructor = (from cons in type.GetConstructors()
                               orderby cons.GetParameters().Length descending
                               select cons).FirstOrDefault();
            if(constructor != null)
            {
                _loggingChannel.Log("Found constructor that can be used to create the domain object.".ToString);

                return from par in constructor.GetParameters()
                       select GetMemberInfo(type, par.Name); 
            }

            var createMethod = (from staticMethod in type.GetMethods( BindingFlags.Static | BindingFlags.Public )
                                where staticMethod.Name.StartsWith( "From" )
                                select staticMethod).FirstOrDefault();
            if(createMethod != null)
            {
                _loggingChannel.Log($"Found static create method {createMethod.Name}".ToString);
                _nameRegistry.RegisterConstructionMethod( type, createMethod );
                return DetermineStructurePropertiesForConstructionMethod( type, createMethod );
            }

            throw new InvalidOperationException(
                $"Type {type.Name} does not have any public constructors and we did not find any suitable static create method. It therefore cannot be used for code generation." );
        }

        private IEnumerable<MemberInfo> DetermineStructurePropertiesForConstructionMethod( Type type,
            MethodInfo createMethod )
        {
            IEnumerable<MemberInfo> propertiesInCorrectOrder;
            if(createMethod.GetParameters().Length == 1)
            {
                var suffix = createMethod.Name.Substring( 4 );
                var property = type.GetProperties().FirstOrDefault( p => p.Name.EndsWith( suffix ) )
                               ?? GetPropertyFunction(type, suffix)
                               ?? throw new InvalidOperationException($"Could not find matching property with suffix {suffix}.");
                propertiesInCorrectOrder = Enumerable.Repeat( property, 1 );
            }
            else
            {
                propertiesInCorrectOrder = from par in createMethod.GetParameters()
                                           select type.GetProperty( par.Name.ToPascalCase() ) ??
                                                  GetPropertyFunction(type, par.Name.ToPascalCase()) ??
                                                  throw new InvalidOperationException(
                                                      $"Could not find matching property for create method {createMethod.Name} parameter {par.Name} of type {type.Name}" );
            }

            return propertiesInCorrectOrder;
        }

        private MemberInfo GetPropertyFunction( Type type, string expectedName )
        {
            _loggingChannel.Log($"Could not find a property for {expectedName}, looking for methods without parameters now. Code generated for this may not compile.".ToString, Severity.Warning);
            var exactFunction = type.GetMethod( expectedName );
            if( exactFunction != null && exactFunction.GetParameters().Length == 0 )
            {
                return exactFunction;
            }

            var candidateFunction = type.GetMethods( BindingFlags.Instance | BindingFlags.Public )
                .FirstOrDefault( m => m.Name.EndsWith( expectedName ) && m.GetParameters().Length == 0 );
            if( candidateFunction != null )
            {
                _loggingChannel.Log($"Chosen function {candidateFunction.Name}. This is highly speculative. Expect compile errors. Please refactor your code.".ToString, Severity.Warning);
                return candidateFunction;
            }

            return null;
        }

        public (string, string, string) ParseElementData( MemberInfo member, string prefix = null, string suffix = null )
        {
            var identifier = member.GetCustomAttribute<SilaIdentifierAttribute>()?.Identifier;
            if(identifier == null)
            {
                identifier = GetSuggestedMemberIdentifier( member, prefix, suffix );
            }
            else
            {
                _nameRegistry?.RegisterRename( member.Name, identifier );
            }

            var displayName = member.GetCustomAttribute<SilaDisplayNameAttribute>()?.DisplayName
                              ?? member.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName
                              ?? identifier.ToDisplayName();

            var description = member.GetCustomAttribute<SilaDescriptionAttribute>()?.Description
                              ?? member.GetCustomAttribute<DescriptionAttribute>()?.Description
                              ?? GetDocumentationSummary( member )
                              ?? string.Empty;

            return (identifier, displayName, description);
        }

        private string GetDocumentationSummary( MemberInfo member )
        {
            if(GetDocEntry( member ) is var actualDoc)
            {
                return actualDoc?.Element( XName.Get( "summary" ) )?.Value?.Trim();
            }
            return null;
        }

        private string GetDocumentationSummary( ParameterInfo parameter )
        {
            if(GetDocEntry( parameter.Member ) is var actualDoc && actualDoc != null )
            {
                _loggingChannel.Log($"Found description for parameter {parameter.Name} in documentation".ToString);
                if( parameter.Position == -1 ) // parameter is method return value
                {
                    return actualDoc.Element( XName.Get( "returns" ) )?.Value?.Trim();
                }
                else
                {
                    foreach( var paramElement in actualDoc.Elements( XName.Get( "param" ) ) )
                    {
                        if( paramElement.Attribute( XName.Get( "name" ) )?.Value == parameter.Name )
                        {
                            return paramElement.Value?.Trim();
                        }
                    }
                }
            }
            return null;
        }

        private XElement GetDocEntry( MemberInfo member )
        {
            if(_loadedDocumentation.TryGetValue( (member as TypeInfo ?? member.DeclaringType).Assembly.FullName, out var docs ) && docs.TryGetValue( GetMemberDocIdentifier( member ), out var actualDoc ))
            {
                return actualDoc;
            }
            else
            {
                return null;
            }
        }

        private string GetMemberDocIdentifier( MemberInfo member )
        {
            switch(member)
            {
                case Type type:
                    return $"T:{type.FullName}";
                case PropertyInfo property:
                    return $"P:{property.DeclaringType.FullName}.{property.Name}";
                case FieldInfo field:
                    return $"F:{field.DeclaringType.FullName}.{field.Name}";
                case MethodInfo method:
                    var parameterString = string.Join( ",", method.GetParameters().Select( p => GetDocTypeIdentifier(p.ParameterType) ) );
                    if( !string.IsNullOrEmpty( parameterString ) )
                    {
                        parameterString = $"({parameterString})";
                    }
                    return $"M:{method.DeclaringType.FullName}.{method.Name}{parameterString}";
                default:
                    return string.Empty;
            }
        }

        private string GetDocTypeIdentifier( Type type )
        {
            if( !type.IsGenericType )
            {
                return type.FullName;
            }

            var parameterString = string.Join(",", type.GetGenericArguments().Select( GetDocTypeIdentifier ));
            var typeDefinition = type.GetGenericTypeDefinition();
            var suffixLength = typeDefinition.GetGenericArguments().Length + 1;
            return typeDefinition.FullName.Substring(0, typeDefinition.FullName.Length - suffixLength) + "{" + parameterString + "}";
        }

        private static string GetSuggestedMemberIdentifier( MemberInfo member, string prefix, string suffix )
        {
            var identifier = member.Name;
            if (identifier.Contains( '`' ))
            {
                var indexOfBacktick = identifier.LastIndexOf( '`' );
                identifier = identifier.Substring( 0, indexOfBacktick );
            }
            if(suffix != null && identifier.EndsWith( suffix ))
            {
                identifier = identifier.Substring( 0, identifier.Length - suffix.Length );
            }

            if(prefix != null && identifier.StartsWith( prefix ))
            {
                identifier = identifier.Substring( prefix.Length );
            }

            return identifier;
        }

        private (string, string, string) ParseElementData( ParameterInfo parameter )
        {
            var identifier = parameter.GetCustomAttribute<SilaIdentifierAttribute>()?.Identifier ?? parameter.Name;
            if(string.IsNullOrEmpty( identifier )) identifier = "ReturnValue";

            if(char.IsLower( identifier[0] ))
            {
                identifier = char.ToUpper( identifier[0] ) + identifier.Substring( 1 );
            }

            var displayName = parameter.GetCustomAttribute<SilaDisplayNameAttribute>()?.DisplayName
                              ?? parameter.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName
                              ?? identifier.ToDisplayName();

            var description = parameter.GetCustomAttribute<SilaDescriptionAttribute>()?.Description
                              ?? parameter.GetCustomAttribute<DescriptionAttribute>()?.Description
                              ?? GetDocumentationSummary( parameter )
                              ?? string.Empty;

            return (identifier, displayName, description);
        }

        private FeatureCommand GenerateCommand( MethodInfo methodInfo, ICollection<Type> executeExceptions,
            ICollection<Type> dataTypes )
        {
            _loggingChannel.Log( $"Generate SiLA2 command for method {methodInfo.Name}".ToString );
            var (identifier, displayName, description) = ParseElementData( methodInfo );

            var exceptionIdentifiers = ParseExceptionNamesAndRegister( methodInfo, executeExceptions );
            var (responseType, intermediateType, isObservable) = ParseCommandTypes( methodInfo, identifier );
            var parameters = ParseCommandParameters( methodInfo, dataTypes, identifier, ref isObservable );

            var inlineResponse = methodInfo.ReturnTypeCustomAttributes.IsDefined( typeof( InlineStructAttribute ), false );

            var command = new FeatureCommand
            {
                Identifier = identifier,
                DisplayName = displayName,
                Description = description,
                DefinedExecutionErrors = exceptionIdentifiers.Any() ? exceptionIdentifiers.ToArray() : null,
                Parameter = parameters,
                Response = CreateCommandResponse( identifier, methodInfo.ReturnParameter, responseType, inlineResponse, dataTypes ),
                Observable = isObservable ? FeatureCommandObservable.Yes : FeatureCommandObservable.No
            };
            if(intermediateType != null)
            {
                SetCommandIntermediateResponseType( dataTypes, command, intermediateType, identifier );
            }

            return command;
        }

        private void SetCommandIntermediateResponseType( ICollection<Type> dataTypes, FeatureCommand command,
            Type intermediateType, string identifier )
        {
            command.IntermediateResponse = new SiLAElement[]
            {
                new SiLAElement()
                {
                    Identifier = "Intermediate",
                    DisplayName = "Intermediate",
                    Description = "",
                    DataType = GenerateTypeReference(intermediateType, identifier + ".Intermediate", null, true,
                        dataTypes.Add)
                }
            };
        }

        private SiLAElement[] ParseCommandParameters( MethodInfo methodInfo, ICollection<Type> dataTypes,
            string identifier, ref bool isObservable )
        {
            var parameters = new List<SiLAElement>();
            foreach(var parameterInfo in methodInfo.GetParameters())
            {
                if( parameterInfo.ParameterType == typeof(CancellationToken) )
                {
                    if( !isObservable )
                    {
                        _loggingChannel.Log($"The method {methodInfo.Name} has a parameter with a cancellation token but is not marked observable.".ToString, Severity.Warning );
                        isObservable = true;
                        _nameRegistry.RegisterMethod(identifier, methodInfo);
                    }

                    if( parameterInfo.Position < methodInfo.GetParameters().Length - 1 )
                    {
                        _loggingChannel.Log("The cancellation token should be the last parameter. Generated code for this feature may not compile.".ToString, Severity.Warning);
                    }
                    continue;
                }
                var (parameterId, parameterName, parameterDescription) = ParseElementData( parameterInfo );

                parameters.Add( new SiLAElement
                {
                    Identifier = parameterId,
                    DisplayName = parameterName,
                    Description = parameterDescription ?? string.Empty,
                    DataType = GenerateTypeReference( parameterInfo.ParameterType,
                        identifier + "." + parameterId,
                        new ParameterAttributeReader( parameterInfo ), true, dataTypes.Add )
                } );
            }

            return parameters.ToArray();
        }

        private SiLAElement[] CreateCommandResponse( string commandId, ParameterInfo returnParameter, Type responseType, bool inlineResponse, ICollection<Type> dataTypes )
        {
            if(responseType == typeof( void ))
            {
                return new SiLAElement[0];
            }

            if(inlineResponse && responseType.IsValueType)
            {
                return GenerateStructureElements( responseType, dataTypes.Add, commandId + "." + "Return" );
            }
            else
            {
                var (responseId, responseName, responseDescription) = ParseElementData( returnParameter );
                return new[]
                {
                    new SiLAElement()
                    {
                        Identifier = responseId,
                        DisplayName = responseName,
                        Description = responseDescription ?? string.Empty,
                        DataType = GenerateTypeReference(responseType, commandId + "." + responseId, new ParameterAttributeReader(returnParameter), true,
                            dataTypes.Add)
                    }
                };
            }
        }

        private (Type, Type, bool) ParseCommandTypes( MethodInfo commandMethod, string identifier )
        {
            var isObservable = commandMethod.GetCustomAttribute<ObservableAttribute>() != null;
            var returnType = commandMethod.ReturnType;
            if(returnType.IsGenericType)
            {
                if(typeof(IObservableCommand).IsAssignableFrom(returnType) || typeof(Task).IsAssignableFrom(returnType) )
                {
                    AssertObservable(commandMethod, ref isObservable);
                }
                var typeDefinition = returnType.GetGenericTypeDefinition();
                if(typeDefinition == typeof( IObservableCommand<> ))
                {
                    return (returnType.GetGenericArguments()[0], null, true);
                }

                if(typeDefinition == typeof( IIntermediateObservableCommand<,> ))
                {
                    var arguments = returnType.GetGenericArguments();
                    return (arguments[1], arguments[0], true);
                }

                if(typeDefinition == typeof( IIntermediateObservableCommand<> ))
                {
                    var arguments = returnType.GetGenericArguments();
                    return (typeof( void ), arguments[0], true);
                }

                if( typeDefinition == typeof(Task<>) )
                {
                    _nameRegistry?.RegisterMethod( identifier, commandMethod );
                    return (returnType.GetGenericArguments()[0], null, true);
                }
            }
            else if(returnType == typeof( IObservableCommand ))
            {
                AssertObservable( commandMethod, ref isObservable );
                return (typeof(void), null, true);
            }
            else if( returnType == typeof(Task) )
            {
                AssertObservable( commandMethod, ref isObservable );
                _nameRegistry?.RegisterMethod( identifier, commandMethod );
                return (typeof(void), null, true);
            }

            if( isObservable )
            {
                _nameRegistry?.RegisterMethod( identifier, commandMethod );
            }

            return (returnType, null, isObservable);
        }

        private void AssertObservable(MethodInfo commandMethod, ref bool isObservable)
        {
            if (!isObservable)
            {
                _loggingChannel.Log( $"Warning: The method {commandMethod.Name} seems observable but is not marked observable.".ToString, Severity.Warning);
                isObservable = true;
            }
        }
    }
}
