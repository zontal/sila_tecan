using Common.Logging;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Tecan.Sila2.Generator.Contracts;
using Tecan.Sila2.Generator.Helper;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.Generator.Generators
{
    internal class FeatureDefinitionGeneratorContext
    {

        private readonly ICodeNameRegistry _nameRegistry;
        private readonly Dictionary<string, Dictionary<string, XElement>> _loadedDocumentation = new Dictionary<string, Dictionary<string, XElement>>();
        private readonly ITypeTranslationProvider _translationProvider;
        private readonly ILog _loggingChannel = LogManager.GetLogger<FeatureDefinitionGeneratorContext>();
        private readonly IAmbiguityResolver _ambiguityResolver;
        private FeatureSpec _featureSpec;
        private readonly Type _interfaceType;
        private readonly HashSet<string> _identifiers = new HashSet<string>();
        private readonly List<object> _items = new List<object>();

        private static readonly string[] ArgumentExceptionTypeNames =
        {
            typeof(ArgumentException).FullName,
            typeof(ArgumentNullException).FullName,
            typeof(ArgumentOutOfRangeException).FullName
        };

        public FeatureDefinitionGeneratorContext( ICodeNameRegistry nameRegistry,
            ITypeTranslationProvider translationProvider,
            IAmbiguityResolver ambiguityResolver,
            FeatureSpec featureSpec,
            Type interfaceType )
        {
            _nameRegistry = nameRegistry;
            _translationProvider = translationProvider;
            _ambiguityResolver = ambiguityResolver;
            _featureSpec = featureSpec;
            _interfaceType = interfaceType;
        }

        public object[] GenerateFeature( ICollection<string> namespaceCollector )
        {
            var executeExceptions = new HashSet<Type>();
            var dataTypes = new HashSet<Type>();

            AddInterfaceMembers( _interfaceType, executeExceptions, dataTypes, false );
            namespaceCollector?.Add( _interfaceType.Namespace );

            GenerateExceptions( executeExceptions, namespaceCollector );
            GenerateDataTypes( dataTypes, namespaceCollector );

            return _items.ToArray();
        }

        public void TryLoadDocumentation( Assembly assembly )
        {
            if(_loadedDocumentation.ContainsKey( assembly.FullName ))
            {
                return;
            }
            var documentationPath = Path.ChangeExtension( assembly.Location, ".xml" );
            if(File.Exists( documentationPath ))
            {
                TryLoadDocumentation( assembly, documentationPath );
            }
            else
            {
                var parentFolderPath = Path.Combine( Path.GetDirectoryName( documentationPath ), "..", Path.GetFileName( documentationPath ) );
                if(File.Exists( parentFolderPath ))
                {
                    TryLoadDocumentation( assembly, parentFolderPath );
                }
            }
        }

        private void TryLoadDocumentation( Assembly assembly, string documentationPath )
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
                _loggingChannel.Info( $"Documentation for {assembly.FullName} successfully loaded." );
            }
            catch(Exception e)
            {
                _loggingChannel.Warn( $"Loading documentation for assembly {assembly.FullName} failed: {e.Message}." );
            }
        }

        private void GenerateDataTypes( HashSet<Type> dataTypes,
            ICollection<string> namespaceCollector )
        {
            // to keep the unitelabs browser happy we want to define types in order if we can
            // if the type refers to other types then we want to put it before those types
            var dependsOn = new List<Tuple<Type, Type>>();
            var newItems = new List<Tuple<Type, SiLAElement>>();
            var processQueue = new Queue<Type>( dataTypes );
            while(processQueue.Any() && processQueue.Dequeue() is var dataType && dataType != null)
            {
                var newItem = GenerateDataType( dataType, t =>
                {
                    dependsOn.Add( Tuple.Create( dataType, t ) );
                    if(dataTypes.Add( t ))
                    {
                        processQueue.Enqueue( t );
                    }
                } );
                newItems.Add( Tuple.Create( dataType, newItem ) );
                namespaceCollector?.Add( dataType.Namespace );
            }

            // sort the items as best we can
            // (since we are presented with the types in random order we cannot guarantee that if A depends on B we 
            // will not have seen B for another reason before we got to A. So A and B could be either way round within
            // newItems and we need to sort that out)
            var sortedElements = new List<Tuple<Type, SiLAElement>>();
            foreach(var typeAndItem in newItems)
            {
                var thisType = typeAndItem.Item1;
                int insertPosition = sortedElements.Count;
                for(var i = insertPosition - 1; i >= 0; --i)
                {
                    var whatElementDependsOn = dependsOn.Where( dep => dep.Item1 == thisType ).Select( dep => dep.Item2 )
                        .Distinct();
                    if(whatElementDependsOn.Contains( thisType ))
                    {
                        insertPosition = i;
                    }
                }
                sortedElements.Insert( insertPosition, typeAndItem );
            }

            _items.AddRange(sortedElements.Select(typeAndItem =>
                                          {
                                              object item = typeAndItem.Item2;
                                              return item;
                                          }).ToList());
        }

        private void GenerateExceptions( ICollection<Type> exceptions, ICollection<string> namespaceCollector )
        {
            foreach(var executeException in exceptions)
            {
                var exception = GenerateExecuteException(executeException);
                AddFeatureItem(exception.Identifier, exception);
                namespaceCollector?.Add( executeException.Namespace );
            }
        }

        private void AddFeatureItem(string identifier, object featureItem)
        {
            if (_identifiers.Add( identifier ))
            {
                _items.Add(featureItem);
            }
        }

        private void AddInterfaceMembers( Type interfaceType, ICollection<Type> definedExceptions, ICollection<Type> dataTypes, bool forceSpec )
        {
            foreach(var methodGroup in interfaceType.GetMethods().GroupBy( m => m.Name ))
            {
                var methodInfo = methodGroup.First();
                if (methodGroup.Count() > 1)
                {
                    methodInfo = ChoseOverload(methodGroup);

                    if (methodInfo == null)
                    {
                        continue;
                    }
                }
                AddInterfaceMethod(definedExceptions, dataTypes, forceSpec, methodInfo);
            }

            foreach (var propertyInfo in interfaceType.GetProperties())
            {
                try
                {
                    if (_featureSpec != null && _featureSpec.TryGetInlineFor(propertyInfo.Name, out var inline))
                    {
                        var currentSpec = _featureSpec;
                        _featureSpec = inline;
                        AddInterfaceMembers(propertyInfo.PropertyType, definedExceptions, dataTypes, true);
                        _featureSpec = currentSpec;
                        continue;
                    }
                    AddInterfaceProperty(definedExceptions, dataTypes, forceSpec, propertyInfo);
                }
                catch (NotSupportedException notSupported)
                {
                    _loggingChannel.Error( $"Failed to expose property {propertyInfo.Name}", notSupported );
                }
            }
        }

        private void AddInterfaceProperty(ICollection<Type> definedExceptions, ICollection<Type> dataTypes, bool forceSpec, PropertyInfo propertyInfo)
        {
            if (!typeof(IRequestInterceptor).IsAssignableFrom(propertyInfo.PropertyType))
            {
                var featureProperty = GenerateProperty(propertyInfo, definedExceptions, dataTypes, out var setterCommand);
                if (_featureSpec != null && forceSpec)
                {
                    _featureSpec.Property = _featureSpec.Property.Add(new PropertySpec
                    {
                        Code = propertyInfo.Name,
                        Identifier = featureProperty.Identifier
                    });
                }
                AddFeatureItem(featureProperty.Identifier, featureProperty);
                if (setterCommand != null)
                {
                    AddFeatureItem(setterCommand.Identifier, setterCommand);
                }
            }
            else
            {
                var metadata = GenerateMetadata(propertyInfo, definedExceptions, dataTypes);
                AddFeatureItem(metadata.Identifier, metadata);
            }
        }

        private void AddInterfaceMethod(ICollection<Type> definedExceptions, ICollection<Type> dataTypes, bool forceSpec, MethodInfo methodInfo)
        {
            if (!methodInfo.IsSpecialName)
            {
                try
                {
                    if (_featureSpec != null && _featureSpec.TryGetPropertyFor(methodInfo.Name, out var propertySpec))
                    {
                        var property = GenerateProperty(methodInfo, propertySpec, definedExceptions, dataTypes);
                        AddFeatureItem(property.Identifier, property);
                    }
                    else
                    {
                        var featureCommand = GenerateCommand(methodInfo, definedExceptions, dataTypes);
                        if (_featureSpec != null && forceSpec)
                        {
                            _featureSpec.Command = _featureSpec.Command.Add(new CommandSpec
                            {
                                Code = methodInfo.Name,
                                Identifier = featureCommand.Identifier
                            });
                        }
                        AddFeatureItem(featureCommand.Identifier, featureCommand);
                    }
                }
                catch (NotSupportedException notSupported)
                {
                    _loggingChannel.Error($"Failed to expose method {methodInfo.Name}({string.Join(", ", methodInfo.GetParameters().Select(p => p.ParameterType.Name))})", notSupported);
                }
            }
        }

        private MethodInfo ChoseOverload( IGrouping<string, MethodInfo> methodGroup )
        {
            if(_featureSpec != null && _featureSpec.TryGetCommandFor( methodGroup.Key, out var commandSpec ) && commandSpec.Overload != null)
            {
                return ChoseOverloadBasedOnSpec( methodGroup, commandSpec.Overload );
            }
            return _ambiguityResolver.GetExposedOverload( methodGroup.ToList() );
        }

        private MethodInfo ChoseOverloadBasedOnSpec( IEnumerable<MethodInfo> candidates, string overload )
        {
            if(string.IsNullOrEmpty( overload ))
            {
                return candidates.FirstOrDefault( m => m.GetParameters().Length == 0 );
            }

            var parameters = overload.Split( new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries );
            for(int i = 0; i < parameters.Length; i++)
            {
                parameters[i] = parameters[i].Trim();
            }
            return candidates.FirstOrDefault( m =>
            {
                var param = m.GetParameters();

                if(param.Length != parameters.Length)
                {
                    return false;
                }

                for(int i = 0; i < param.Length; i++)
                {
                    if(!string.Equals( param[i].Name, parameters[i], StringComparison.OrdinalIgnoreCase )
                    && !string.Equals( param[i].ParameterType.Name, parameters[i], StringComparison.OrdinalIgnoreCase )
                    && !string.Equals( param[i].ParameterType.FullName, parameters[i], StringComparison.OrdinalIgnoreCase ))
                    {
                        return false;
                    }
                }

                return true;
            } );
        }

        private FeatureMetadata GenerateMetadata( PropertyInfo propertyInfo, ICollection<Type> definedExceptions, ICollection<Type> dataTypes )
        {
            _loggingChannel.Debug( $"Generate SiLA2 metadata for property {propertyInfo.Name}" );
            var (identifier, displayName, description) = ParseElementData( propertyInfo, null );
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
                DataType = GenerateTypeReference( metadataType.MetadataType, identifier, null, new MemberAttributeReader( propertyInfo ), true,
                    dataTypes.Add )
            };

            return metadata;
        }

        private SiLAElement GenerateDataType( Type dataType, Action<Type> typeAction )
        {
            TypeSpec spec = null;
            _featureSpec?.TryGetTypeFor( dataType.Name, out spec );
            var (id, name, description) = ParseElementData( dataType, spec );

            return new SiLAElement()
            {
                Identifier = id,
                DisplayName = name,
                Description = description,
                DataType = GenerateTypeReference( dataType, dataType.Name, null, new MemberAttributeReader( dataType ), false, typeAction )
            };
        }

        private FeatureDefinedExecutionError GenerateExecuteException( Type exception )
        {
            var (identifier, displayName, description) = ParseElementData( exception, null, suffix: nameof( Exception ) );

            return new FeatureDefinedExecutionError
            {
                Identifier = identifier,
                Description = description,
                DisplayName = displayName
            };
        }

        private Type Follow( Type baseType, Expression expression, ref string origin )
        {
            switch(expression)
            {
                case null:
                    return baseType;
                case PropertyExpression property:
                    var actualProperty = baseType.GetProperty( property.Property );
                    origin += "." + property.Property;
                    return actualProperty.PropertyType;
                case FormatExpression:
                    return typeof( string );
                default:
                    throw new NotImplementedException();
            }
        }

        private FeatureProperty GenerateProperty( MethodInfo method, PropertySpec spec, ICollection<Type> readExceptions,
            ICollection<Type> dataTypes )
        {
            if(method.GetParameters().Length > 0)
            {
                throw new InvalidOperationException( $"Method {method.Name} is configured to be generated as property but has parameters." );
            }

            _loggingChannel.Debug( $"Generate SiLA2 property for {method.Name}" );
            var (identifier, displayName, description) = ParseElementData( method, spec );

            var definedErrors = ParseExceptionNamesAndRegister( method, readExceptions );
            var isDynamic = method.GetCustomAttribute<ObservableAttribute>() != null;
            var property = new FeatureProperty
            {
                Identifier = identifier,
                DisplayName = displayName,
                Description = description,
                DataType = GenerateTypeReference( method.ReturnType, identifier, spec?.Mapping, new MemberAttributeReader( method ), true,
                    dataTypes.Add ),
                DefinedExecutionErrors = definedErrors.Any() ? definedErrors.ToArray() : null,
                Observable = isDynamic ? FeaturePropertyObservable.Yes : FeaturePropertyObservable.No
            };
            _nameRegistry?.RegisterMethod( identifier, method );
            return property;
        }

        private FeatureProperty GenerateProperty( PropertyInfo propertyInfo, ICollection<Type> readExceptions,
            ICollection<Type> dataTypes, out FeatureCommand setterCommand )
        {
            PropertySpec spec = null;
            _featureSpec?.TryGetPropertyFor( propertyInfo.Name, out spec );

            _loggingChannel.Debug( $"Generate SiLA2 property for {propertyInfo.Name}" );
            var (identifier, displayName, description) = ParseElementData( propertyInfo, spec );

            var definedErrors = ParseExceptionNamesAndRegister( propertyInfo, readExceptions );
            var isDynamic = propertyInfo.GetCustomAttribute<ObservableAttribute>() != null;
            setterCommand = null;
            var property = new FeatureProperty
            {
                Identifier = identifier,
                DisplayName = displayName,
                Description = description,
                DataType = GenerateTypeReference( propertyInfo.PropertyType, identifier, spec?.Mapping, new MemberAttributeReader( propertyInfo ), true,
                    dataTypes.Add ),
                DefinedExecutionErrors = definedErrors.Any() ? definedErrors.ToArray() : null,
                Observable = isDynamic ? FeaturePropertyObservable.Yes : FeaturePropertyObservable.No
            };
            if(propertyInfo.CanWrite)
            {
                _loggingChannel.Debug( $"Property {propertyInfo.Name} is writable, generating setter command." );
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
                _loggingChannel.Debug( $"Found documentation for {member.Name}, extracting exceptions." );
                var assembly = member.DeclaringType.Assembly;

                foreach(var exceptionElement in docEntry.Elements( XName.Get( "exception" ) ))
                {
                    ProcessExceptionDoc(exceptions, errors, assembly, exceptionElement);
                }
            }

            return errors;
        }

        private void ProcessExceptionDoc(ICollection<Type> exceptions, HashSet<string> errors, Assembly assembly, XElement exceptionElement)
        {
            var typeName = exceptionElement.Attribute(XName.Get("cref"))?.Value?.Substring(2);
            if (typeName != null)
            {
                if (ArgumentExceptionTypeNames.Contains(typeName) || typeof(Exception).FullName.Equals(typeName))
                {
                    _loggingChannel.Debug($"Ignoring exception {typeName} because it is covered by SiLA2 validation errors.");
                    return;
                }
                Type exceptionType = FindExceptionType(assembly, typeName);
                if (exceptionType != null)
                {
                    _loggingChannel.Debug($"Found exception type {exceptionType.Name}");
                    AddError(exceptions, exceptionType, errors);
                    return;
                }
            }
            _loggingChannel.Warn($"Exception {typeName} could not be found.");
        }

        private static Type FindExceptionType(Assembly assembly, string typeName)
        {
            var exceptionType = assembly.GetType(typeName, false);
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

            return exceptionType;
        }

        private void AddError( ICollection<Type> exceptions, Type exceptionType, ICollection<string> errors )
        {
            exceptions.Add( exceptionType );
            var (identifier, _, _) = ParseElementData( exceptionType, null, suffix: nameof( Exception ) );
            errors.Add( identifier );
        }

        private DataTypeType GenerateTypeReference( Type type, string origin, TypeMapping mapping, IAttributeReader reader, bool allowReference,
            Action<Type> typeAction )
        {
            if(mapping?.ValueExpression != null)
            {
                _nameRegistry?.RegisterDifferentType( origin, type );
                type = Follow( type, mapping.ValueExpression, ref origin );
            }

            if(type.Namespace.EndsWith( "." + type.Name ))
            {
                _nameRegistry?.RegisterDifferentType( origin, type );
            }

            if(!_translationProvider.TryTranslate( type, origin, out var resultType ))
            {
                throw new NotSupportedException( $"There was no translation found for type {type.FullName}. " );
            }

            if(typeAction != null)
            {
                _translationProvider.TraverseTypes( type, origin, typeAction );
            }

            if(!allowReference && resultType.Item is string)
            {
                if(type.IsEnum)
                {
                    return GenerateEnum( type );
                }

                return GenerateStructure( type, mapping, typeAction );
            }

            if(reader != null)
            {
                resultType = AsConstrained( reader, resultType, mapping?.Constraint );
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
                if(components.Count == 0)
                {
                    components.Add( new ConstraintsUnitUnitComponent
                    {
                        SIUnit = ConstraintsUnitUnitComponentSIUnit.Dimensionless,
                        Exponent = "0"
                    } );
                }
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
                if(contentType.Parameters != null)
                {
                    constraints.ContentType.Parameters = contentType.Parameters.Select( p => new ConstraintsContentTypeParameter
                    {
                        Attribute = p.Key,
                        Value = p.Value
                    } ).ToArray();
                }
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
                    case IdentifierType.MetadataIdentifier:
                        constraints.FullyQualifiedIdentifier = ConstraintsFullyQualifiedIdentifier.MetadataIdentifier;
                        break;
                    case IdentifierType.IntermediateResponseIdentifier:
                        constraints.FullyQualifiedIdentifier = ConstraintsFullyQualifiedIdentifier.IntermediateCommandResponseIdentifier;
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

        private DataTypeType GenerateStructure( Type type, TypeMapping mapping, Action<Type> typeFoundAction )
        {
            var identifier = type.GetCustomAttribute<SilaIdentifierAttribute>()?.Identifier ?? type.Name;
            var properties = type.GetProperties();
            if(properties.Length == 1 && properties[0].Name == "Value")
            {
                var prop = properties[0];
                return GenerateTypeReference( prop.PropertyType, identifier + "." + prop.Name, mapping, new MemberAttributeReader( type ), true, null );
            }

            TypeSpec spec = null;
            _featureSpec?.TryGetTypeFor( type.Name, out spec );

            return new DataTypeType
            {
                Item = new StructureType
                {
                    Element = GenerateStructureElements( type, spec?.Property, typeFoundAction, identifier )
                }
            };
        }

        private SiLAElement[] GenerateStructureElements( Type type, PropertyMapping[] spec, Action<Type> typeFoundAction, string identifier )
        {
            return (from prop in DetermineStructureProperties( type )
                    let mapping = GetMapping( spec, prop )
                    select new SiLAElement
                    {
                        DataType = GenerateTypeReference( (prop as PropertyInfo)?.PropertyType ?? (prop as FieldInfo)?.FieldType ?? (prop as MethodInfo)?.ReturnType, identifier + "." + prop.Name, mapping?.Mapping, new MemberAttributeReader( prop ),
                            true, typeFoundAction ),
                        Identifier = prop.GetCustomAttribute<SilaIdentifierAttribute>()?.Identifier ?? mapping?.Identifier ?? prop.Name,
                        DisplayName = prop.GetCustomAttribute<SilaDisplayNameAttribute>()?.DisplayName ??
                                      prop.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ??
                                      prop.Name,
                        Description = prop.GetCustomAttribute<SilaDescriptionAttribute>()?.Description ??
                                      prop.GetCustomAttribute<DescriptionAttribute>()?.Description ??
                                      GetDocumentationSummary( prop ) ?? string.Empty
                    }).ToArray();
        }

        private PropertyMapping GetMapping( PropertyMapping[] spec, MemberInfo prop )
        {
            if(spec == null)
            {
                return null;
            }

            return spec.FirstOrDefault( m => m.Property == prop.Name );
        }

        private MemberInfo GetMemberInfo( MemberInfo[] memberInfos, string parameterName )
        {
            if(memberInfos.FirstOrDefault( x => x.Name.StartsWith( parameterName, StringComparison.OrdinalIgnoreCase ) ) is var member && member != null)
            {
                return member;
            }
            if(memberInfos.Length == 1)
            {
                return memberInfos[0];
            }

            return null;
        }

        private MemberInfo GetMemberInfo( Type type, string parameterName )
        {
            if(type.GetProperty( parameterName.ToPascalCase() ) is var property && property != null)
            {
                return property;
            }
            if(type.GetField( parameterName.ToPascalCase() ) is var field && field != null)
            {
                return field;
            }
            if(GetMemberInfo( type.GetProperties(), parameterName ) is var memberProperty && memberProperty != null)
            {
                return memberProperty;
            }
            if(GetMemberInfo( type.GetFields(), parameterName ) is var memberField && memberField != null)
            {
                return memberField;
            }

            if(GetTypeSpec( type ) is TypeSpec typeSpec
                && typeSpec.TryGetMappingFor( parameterName, out var mapping )
                && mapping.Property != null)
            {

                return GetMemberInfo( type, mapping.Property );
            }

            List<MemberInfo> allowedProperties = new List<MemberInfo>( type.GetProperties() );
            allowedProperties.AddRange( type.GetFields() );
            if(_ambiguityResolver?.GetUserDefinedProperty( type, parameterName, allowedProperties ) is var userDefinedProperty && userDefinedProperty != null)
            {
                return userDefinedProperty;
            }
            throw new NotSupportedException( $"Could not find matching property for constructor parameter {parameterName} of type {type.Name}" );
        }

        private TypeSpec GetTypeSpec( Type type )
        {
            if(_featureSpec != null
                            && _featureSpec.TryGetTypeFor( type.Name, out var typeSpec ))
            {
                return typeSpec;
            }
            return null;
        }

        private IEnumerable<MemberInfo> DetermineStructureProperties( Type type )
        {
            var constructor = (from cons in type.GetConstructors()
                               orderby cons.GetParameters().Length descending
                               select cons).FirstOrDefault();
            if(constructor != null)
            {
                _loggingChannel.Debug( "Found constructor that can be used to create the domain object." );

                return from par in constructor.GetParameters()
                       select GetMemberInfo( type, par.Name );
            }

            var createMethod = (from staticMethod in type.GetMethods( BindingFlags.Static | BindingFlags.Public )
                                where staticMethod.Name.StartsWith( "From" )
                                select staticMethod).FirstOrDefault();
            if(createMethod != null)
            {
                _loggingChannel.Debug( $"Found static create method {createMethod.Name}" );
                _nameRegistry?.RegisterConstructionMethod( type, createMethod );
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
                               ?? GetPropertyFunction( type, suffix )
                               ?? throw new InvalidOperationException( $"Could not find matching property with suffix {suffix}." );
                propertiesInCorrectOrder = Enumerable.Repeat( property, 1 );
            }
            else
            {
                propertiesInCorrectOrder = from par in createMethod.GetParameters()
                                           select type.GetProperty( par.Name.ToPascalCase() ) ??
                                                  GetPropertyFunction( type, par.Name.ToPascalCase() ) ??
                                                  throw new InvalidOperationException(
                                                      $"Could not find matching property for create method {createMethod.Name} parameter {par.Name} of type {type.Name}" );
            }

            return propertiesInCorrectOrder;
        }

        private MemberInfo GetPropertyFunction( Type type, string expectedName )
        {
            _loggingChannel.Warn( $"Could not find a property for {expectedName}, looking for methods without parameters now. Code generated for this may not compile." );
            var exactFunction = type.GetMethod( expectedName );
            if(exactFunction != null && exactFunction.GetParameters().Length == 0)
            {
                return exactFunction;
            }

            var candidateFunction = type.GetMethods( BindingFlags.Instance | BindingFlags.Public )
                .FirstOrDefault( m => m.Name.EndsWith( expectedName ) && m.GetParameters().Length == 0 );
            if(candidateFunction != null)
            {
                _loggingChannel.Warn( $"Chosen function {candidateFunction.Name}. This is highly speculative. Expect compile errors. Please refactor your code." );
                return candidateFunction;
            }

            return null;
        }

        public (string, string, string) ParseElementData( MemberInfo member, MemberSpec spec, string prefix = null, string suffix = null )
        {
            var identifier = member.GetCustomAttribute<SilaIdentifierAttribute>()?.Identifier ?? spec?.Identifier;
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
                              ?? spec?.Description
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
            if(GetDocEntry( parameter.Member ) is var actualDoc && actualDoc != null)
            {
                _loggingChannel.Debug( $"Found description for parameter {parameter.Name} in documentation" );
                if(parameter.Position == -1) // parameter is method return value
                {
                    return actualDoc.Element( XName.Get( "returns" ) )?.Value?.Trim();
                }
                else
                {
                    foreach(var paramElement in actualDoc.Elements( XName.Get( "param" ) ))
                    {
                        if(paramElement.Attribute( XName.Get( "name" ) )?.Value == parameter.Name)
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
                    var parameterString = string.Join( ",", method.GetParameters().Select( p => GetDocTypeIdentifier( p.ParameterType ) ) );
                    if(!string.IsNullOrEmpty( parameterString ))
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
            if(!type.IsGenericType)
            {
                return type.FullName;
            }

            var parameterString = string.Join( ",", type.GetGenericArguments().Select( GetDocTypeIdentifier ) );
            var typeDefinition = type.GetGenericTypeDefinition();
            var suffixLength = typeDefinition.GetGenericArguments().Length + 1;
            return typeDefinition.FullName.Substring( 0, typeDefinition.FullName.Length - suffixLength ) + "{" + parameterString + "}";
        }

        private static string GetSuggestedMemberIdentifier( MemberInfo member, string prefix, string suffix )
        {
            var identifier = member.Name;
            if(identifier.Contains( '`' ))
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

        private (string, string, string) ParseElementData( ParameterInfo parameter, PropertyMapping mapping )
        {
            var identifier = parameter.GetCustomAttribute<SilaIdentifierAttribute>()?.Identifier ?? mapping?.Identifier ?? parameter.Name;
            if(string.IsNullOrEmpty( identifier )) identifier = "ReturnValue";

            if(char.IsLower( identifier[0] ))
            {
                identifier = char.ToUpper( identifier[0] ) + identifier.Substring( 1 );
            }

            var displayName = parameter.GetCustomAttribute<SilaDisplayNameAttribute>()?.DisplayName
                              ?? mapping?.DisplayName
                              ?? parameter.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName
                              ?? identifier.ToDisplayName();

            var description = parameter.GetCustomAttribute<SilaDescriptionAttribute>()?.Description
                              ?? mapping?.Description
                              ?? parameter.GetCustomAttribute<DescriptionAttribute>()?.Description
                              ?? GetDocumentationSummary( parameter )
                              ?? string.Empty;

            return (identifier, displayName, description);
        }

        private FeatureCommand GenerateCommand( MethodInfo methodInfo, ICollection<Type> executeExceptions,
            ICollection<Type> dataTypes )
        {
            _loggingChannel.Debug( $"Generate SiLA2 command for method {methodInfo.Name}" );
            CommandSpec spec = null;
            _featureSpec?.TryGetCommandFor( methodInfo.Name, out spec );
            var (identifier, displayName, description) = ParseElementData( methodInfo, spec );

            var exceptionIdentifiers = ParseExceptionNamesAndRegister( methodInfo, executeExceptions );
            var (responseType, intermediateType, isObservable) = ParseCommandTypes( methodInfo, identifier );
            if(spec != null && spec.ObservableSpecified && isObservable != spec.Observable)
            {
                if(!isObservable)
                {
                    isObservable = true;
                }
                else
                {
                    throw new InvalidOperationException( $"Command {identifier} generated for {_interfaceType.Name} is specified to be not observable but the signature demands it." );
                }
            }
            var parameters = ParseCommandParameters( methodInfo, spec, dataTypes, identifier, ref isObservable );

            var inlineResponse = methodInfo.ReturnTypeCustomAttributes.IsDefined( typeof( InlineStructAttribute ), false );

            var command = new FeatureCommand
            {
                Identifier = identifier,
                DisplayName = displayName,
                Description = description,
                DefinedExecutionErrors = exceptionIdentifiers.Any() ? exceptionIdentifiers.ToArray() : null,
                Parameter = parameters,
                Response = CreateCommandResponse( identifier, methodInfo.ReturnParameter, spec, responseType, inlineResponse, dataTypes ),
                Observable = isObservable ? FeatureCommandObservable.Yes : FeatureCommandObservable.No
            };
            if(intermediateType != null)
            {
                SetCommandIntermediateResponseType( dataTypes, command, spec, intermediateType, identifier );
            }

            return command;
        }

        private void SetCommandIntermediateResponseType( ICollection<Type> dataTypes, FeatureCommand command, CommandSpec spec,
            Type intermediateType, string identifier )
        {
            command.IntermediateResponse = new SiLAElement[]
            {
                new SiLAElement()
                {
                    Identifier = "Intermediate",
                    DisplayName = "Intermediate",
                    Description = "",
                    DataType = GenerateTypeReference(intermediateType, identifier + ".Intermediate", spec?.IntermediateResponse?.Mapping, null, true,
                        dataTypes.Add)
                }
            };
        }

        private SiLAElement[] ParseCommandParameters( MethodInfo methodInfo, CommandSpec spec, ICollection<Type> dataTypes,
            string identifier, ref bool isObservable )
        {
            var parameters = new List<SiLAElement>();
            foreach(var parameterInfo in methodInfo.GetParameters())
            {
                if(parameterInfo.ParameterType == typeof( CancellationToken ))
                {
                    if(!isObservable)
                    {
                        _loggingChannel.Warn( $"The method {methodInfo.Name} has a parameter with a cancellation token but is not marked observable." );
                        isObservable = true;
                        _nameRegistry?.RegisterMethod( identifier, methodInfo );
                    }

                    if(parameterInfo.Position < methodInfo.GetParameters().Length - 1)
                    {
                        _loggingChannel.Warn( "The cancellation token should be the last parameter. Generated code for this feature may not compile." );
                    }
                    continue;
                }
                var parameterSpec = spec?.Parameter?.FirstOrDefault( p => p.Key == parameterInfo.Name );
                var (parameterId, parameterName, parameterDescription) = ParseElementData( parameterInfo, parameterSpec );

                parameters.Add( new SiLAElement
                {
                    Identifier = parameterId,
                    DisplayName = parameterName,
                    Description = parameterDescription ?? string.Empty,
                    DataType = GenerateTypeReference( parameterInfo.ParameterType,
                        identifier + "." + parameterId,
                        parameterSpec?.Mapping,
                        new ParameterAttributeReader( parameterInfo ), true, dataTypes.Add )
                } );
            }

            return parameters.ToArray();
        }

        private SiLAElement[] CreateCommandResponse( string commandId, ParameterInfo returnParameter, CommandSpec spec, Type responseType, bool inlineResponse, ICollection<Type> dataTypes )
        {
            if(responseType == typeof( void ))
            {
                return new SiLAElement[0];
            }

            if(inlineResponse && responseType.IsValueType)
            {
                return GenerateStructureElements( responseType, spec?.Parameter, dataTypes.Add, commandId + "." + "Return" );
            }
            else
            {
                var returnSpec = spec?.Response?.FirstOrDefault();
                var (responseId, responseName, responseDescription) = ParseElementData( returnParameter, returnSpec );
                return new[]
                {
                    new SiLAElement()
                    {
                        Identifier = responseId,
                        DisplayName = responseName,
                        Description = responseDescription ?? string.Empty,
                        DataType = GenerateTypeReference(responseType, commandId + "." + responseId, returnSpec?.Mapping, new ParameterAttributeReader(returnParameter), true,
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
                if(typeof( IObservableCommand ).IsAssignableFrom( returnType ) || typeof( Task ).IsAssignableFrom( returnType ))
                {
                    AssertObservable( commandMethod, ref isObservable );
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

                if(typeDefinition == typeof( Task<> ))
                {
                    _nameRegistry?.RegisterMethod( identifier, commandMethod );
                    return (returnType.GetGenericArguments()[0], null, true);
                }

                if(typeDefinition == typeof( IObservable<> ))
                {
                    _nameRegistry?.RegisterMethod( identifier, commandMethod );
                    return (typeof( void ), returnType.GetGenericArguments()[0], true);
                }
            }
            else if(returnType == typeof( IObservableCommand ))
            {
                AssertObservable( commandMethod, ref isObservable );
                return (typeof( void ), null, true);
            }
            else if(returnType == typeof( Task ))
            {
                AssertObservable( commandMethod, ref isObservable );
                _nameRegistry?.RegisterMethod( identifier, commandMethod );
                return (typeof( void ), null, true);
            }

            if(isObservable)
            {
                _nameRegistry?.RegisterMethod( identifier, commandMethod );
            }

            return (returnType, null, isObservable);
        }

        private void AssertObservable( MethodInfo commandMethod, ref bool isObservable )
        {
            if(!isObservable)
            {
                _loggingChannel.Warn( $"Warning: The method {commandMethod.Name} seems observable but is not marked observable." );
                isObservable = true;
            }
        }
    }
}
