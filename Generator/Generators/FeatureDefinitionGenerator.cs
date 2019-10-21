using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using Tecan.Sila2.Generator.Contracts;
using Tecan.Sila2.Generator.Helper;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.Generator.Generators
{
    /// <summary>
    /// Generates features from a given interface
    /// </summary>
    [Export(typeof(IFeatureDefinitionGenerator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class FeatureDefinitionGenerator : IFeatureDefinitionGenerator
    {
        private static readonly Dictionary<Type, (BasicType Type, bool IsNonDefault)> BasicTypes = new Dictionary<Type, (BasicType, bool)>
        {
            {typeof(string), (BasicType.String, false)},
            {typeof(byte), (BasicType.Integer, true)},
            {typeof(sbyte), (BasicType.Integer, true)},
            {typeof(ushort), (BasicType.Integer, true)},
            {typeof(short), (BasicType.Integer, true)},
            {typeof(uint), (BasicType.Integer, true)},
            {typeof(int), (BasicType.Integer, true)},
            {typeof(ulong), (BasicType.Integer, true)},
            {typeof(long), (BasicType.Integer, false)},
            {typeof(float), (BasicType.Real, true)},
            {typeof(double), (BasicType.Real, false)},
            {typeof(bool), (BasicType.Boolean, false)},
            {typeof(byte[]), (BasicType.Binary, true)},
            {typeof(DateTimeOffset), (BasicType.Timestamp, false)},
            {typeof(DateTime), (BasicType.Timestamp, true)},
            {typeof(TimeSpan), (BasicType.Time, false)},
            {typeof(FileInfo), (BasicType.Binary, true)},
            {typeof(Stream), (BasicType.Binary, false)}
        };

        /// <summary>
        /// A registry to which any deviations from suggested rules are reported
        /// </summary>
        public ICodeNameRegistry NameRegistry { get; }

        private IEnumerable<IGeneratorHook> Hooks { get; }

        /// <summary>
        /// Creates a new feature definition generator for the provided registry
        /// </summary>
        /// <param name="nameRegistry">A component that accepts rename reports</param>
        /// <param name="hooks">Generator hooks</param>
        [ImportingConstructor]
        public FeatureDefinitionGenerator(ICodeNameRegistry nameRegistry, [ImportMany] IEnumerable<IGeneratorHook> hooks)
        {
            NameRegistry = nameRegistry;
            Hooks = hooks;
        }

        /// <inheritdoc />
        public Feature GenerateFeature(Type interfaceType, string originator = null, string category = null,
            string version = null, ICollection<string> namespaceCollector = null)
        {
            if (interfaceType == null) throw new ArgumentNullException(nameof(interfaceType));
            var annotation = interfaceType.GetCustomAttribute<SilaFeatureAttribute>();
            if (annotation == null)
                throw new ArgumentException("Interface must be annotated as SiLA feature", nameof(interfaceType));

            var draft = annotation.IsDraft;
            var (identifier, displayName, description) = ParseElementData(interfaceType, prefix: "I");

            var items = new List<object>();
            var executeExceptions = new HashSet<Type>();
            var dataTypes = new HashSet<Type>();

            AddInterfaceMembers(interfaceType, items, executeExceptions, dataTypes);
            namespaceCollector?.Add(interfaceType.Namespace);

            GenerateExceptions(executeExceptions, items, namespaceCollector);
            GenerateDataTypes(dataTypes, items, namespaceCollector);

            var versionV = version == null ? interfaceType.Assembly.GetName().Version : Version.Parse(version);
            category = category ?? annotation.Category;
            AssignDefaultOriginatorAndUseCase(interfaceType, versionV, ref originator, ref category);

            var feature = new Feature
            {
                Description = description,
                DisplayName = displayName,
                Identifier = identifier,
                Items = items.ToArray(),
                SiLA2Version = "1.0",
                FeatureVersion = versionV.ToString(2),
                MaturityLevel = draft ? FeatureMaturityLevel.Draft : FeatureMaturityLevel.Verified,
                Originator = originator.ToLowerInvariant(),
                Category = category.ToLowerInvariant()
            };

            if (Hooks != null)
            {
                foreach (var hook in Hooks)
                {
                    hook?.OnFeatureGenerated(interfaceType, feature);
                }
            }

            return feature;
        }

        private void GenerateDataTypes(HashSet<Type> dataTypes, List<object> items,
            ICollection<string> namespaceCollector)
        {
            var processDataTypes = new Queue<Type>(dataTypes);
            while (processDataTypes.Count > 0)
            {
                var dataType = processDataTypes.Dequeue();
                if (!BasicTypes.ContainsKey(dataType))
                {
                    items.Add(GenerateDataType(dataType, t =>
                    {
                        if (dataTypes.Add(t)) processDataTypes.Enqueue(t);
                    }));
                    namespaceCollector?.Add(dataType.Namespace);
                }
            }
        }

        private void GenerateExceptions(ICollection<Type> exceptions, ICollection<object> items,
            ICollection<string> namespaceCollector)
        {
            foreach (var executeException in exceptions)
            {
                items.Add(GenerateExecuteException(executeException));
                namespaceCollector?.Add(executeException.Namespace);
            }
        }

        private void AddInterfaceMembers(Type interfaceType, ICollection<object> items,
            ICollection<Type> definedExceptions, ICollection<Type> dataTypes)
        {
            foreach (var methodInfo in interfaceType.GetMethods())
            {
                if (!methodInfo.IsSpecialName)
                {
                    items.Add(GenerateCommand(methodInfo, definedExceptions, dataTypes));
                }
            }

            foreach (var propertyInfo in interfaceType.GetProperties())
            {
                if (!typeof(IRequestInterceptor).IsAssignableFrom(propertyInfo.PropertyType))
                {
                    items.Add(GenerateProperty(propertyInfo, definedExceptions, dataTypes));
                }
                else
                {
                    items.Add(GenerateMetadata(propertyInfo, definedExceptions, dataTypes));
                }
            }
        }

        private FeatureMetadata GenerateMetadata(PropertyInfo propertyInfo, ICollection<Type> definedExceptions, ICollection<Type> dataTypes)
        {
            var (identifier, displayName, description) = ParseElementData(propertyInfo);
            var exceptionIdentifiers = ParseExceptionNamesAndRegister(propertyInfo, definedExceptions);

            var metadataType = propertyInfo.GetCustomAttribute<MetadataTypeAttribute>();
            if (metadataType == null)
            {
                throw new InvalidOperationException($"The property {propertyInfo.Name} misses the required MetadataType attribute.");
            }

            var metadata = new FeatureMetadata
            {
                Identifier = identifier,
                DisplayName = displayName,
                Description = description,
                DefinedExecutionErrors = exceptionIdentifiers.Any() ? exceptionIdentifiers.ToArray() : null,
                DataType = GenerateTypeReferenceAndRegister(metadataType.MetadataType, identifier, propertyInfo,
                    dataTypes)
            };

            return metadata;
        }

        private static void AssignDefaultOriginatorAndUseCase(Type interfaceType, Version version,
            ref string originator,
            ref string useCase)
        {
            if (originator == null || useCase == null)
            {
                var ns = interfaceType.FullName.Substring(0,
                    interfaceType.FullName.Length - interfaceType.Name.Length - 1);
                var useCaseStartIndex = 0;
                var endIndex = ns.Length;
                var versionSuffix = "v" + version.Major.ToString();
                if (ns.EndsWith(versionSuffix))
                {
                    endIndex -= versionSuffix.Length + 1;
                }

                if (useCase != null)
                {
                    useCaseStartIndex = ns.IndexOf(useCase, StringComparison.OrdinalIgnoreCase);
                    if (useCaseStartIndex == -1) originator = ns;
                }
                else if (originator != null)
                {
                    var originatorStartIndex = ns.IndexOf(originator, StringComparison.OrdinalIgnoreCase);
                    useCaseStartIndex = originatorStartIndex + originator.Length + 1;
                }
                else
                {
                    useCaseStartIndex = ns.LastIndexOf(".", StringComparison.Ordinal) + 1;
                }

                if (originator == null) originator = ns.Substring(0, useCaseStartIndex - 1);
                if (useCase == null) useCase = ns.Substring(useCaseStartIndex, endIndex - useCaseStartIndex);
            }
        }

        private SiLAElement GenerateDataType(Type dataType, Action<Type> registry)
        {
            var (id, name, description) = ParseElementData(dataType);

            return new SiLAElement()
            {
                Identifier = id,
                DisplayName = name,
                Description = description,
                DataType = GenerateTypeReference(dataType, null, dataType, false, registry)
            };
        }

        private FeatureDefinedExecutionError GenerateExecuteException(Type exception)
        {
            var (identifier, displayName, description) = ParseElementData(exception, suffix: nameof(Exception));

            return new FeatureDefinedExecutionError
            {
                Identifier = identifier,
                Description = description,
                DisplayName = displayName
            };
        }

        private FeatureProperty GenerateProperty(PropertyInfo propertyInfo, ICollection<Type> readExceptions,
            ICollection<Type> dataTypes)
        {
            var (identifier, displayName, description) = ParseElementData(propertyInfo);

            var definedErrors = ParseExceptionNamesAndRegister(propertyInfo, readExceptions);
            var isDynamic = propertyInfo.GetCustomAttribute<DynamicAttribute>() != null;
            return new FeatureProperty
            {
                Identifier = identifier,
                DisplayName = displayName,
                Description = description,
                DataType = GenerateTypeReferenceAndRegister(propertyInfo.PropertyType, identifier, propertyInfo,
                    dataTypes),
                DefinedExecutionErrors = definedErrors.Any() ? definedErrors.ToArray() : null,
                Observable = isDynamic ? FeaturePropertyObservable.Yes : FeaturePropertyObservable.No
            };
        }

        private static (Type, bool) ExtractType(Type type)
        {
            var isCollection = type.IsGenericType && 
                (type.GetGenericTypeDefinition() == typeof(ICollection<>)
                || type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                || type.GetGenericTypeDefinition() == typeof(IList<>));
            if (isCollection)
            {
                type = type.GetGenericArguments()[0];
            }

            return (type, isCollection);
        }

        private List<string> ParseExceptionNamesAndRegister(MemberInfo member, ICollection<Type> exceptions)
        {
            var errors = new List<string>();
            foreach (var throwAtt in member.GetCustomAttributes<ThrowsAttribute>())
            {
                exceptions.Add(throwAtt.ExceptionType);
                var (identifier, _, _) = ParseElementData(throwAtt.ExceptionType, suffix: nameof(Exception));
                errors.Add(identifier);
            }

            return errors;
        }

        private DataTypeType GenerateTypeReferenceAndRegister(Type type, string origin, MemberInfo member,
            ICollection<Type> registry)
        {
            var (innerType, isCollection) = ExtractType(type);
            registry.Add(innerType);
            var dataType = GenerateTypeReference(innerType, origin, member, true, registry.Add);
            if (isCollection)
            {
                if (type.GetGenericTypeDefinition() != typeof(ICollection<>))
                {
                    NameRegistry?.RegisterDifferentType(origin, type);
                }
                return AsCollection(dataType);
            }
            else
            {
                return dataType;
            }
        }

        private DataTypeType GenerateTypeReference(Type type, string origin, MemberInfo member, bool allowReference,
            Action<Type> typeAction)
        {
            DataTypeType resultType = new DataTypeType();

            if (BasicTypes.TryGetValue(type, out var basic))
            {
                resultType.Item = basic.Type;
                if (basic.IsNonDefault)
                {
                    NameRegistry?.RegisterDifferentType(origin, type);
                }
            }
            else if (IsCollectionType(type, out var elementType))
            {
                resultType.Item = new ListType()
                {
                    DataType = GenerateTypeReference(elementType, origin + ".Item", member, allowReference, typeAction)
                };
            }
            else
            {
                typeAction?.Invoke(type);
                if (allowReference)
                {
                    resultType.Item = type.GetCustomAttribute<SilaIdentifierAttribute>()?.Identifier ?? type.Name;
                }
                else
                {
                    if (type.IsEnum)
                    {
                        return GenerateEnum(type);
                    }

                    return GenerateStructure(type, typeAction);
                }
            }

            if (member != null)
            {
                resultType = AsConstrained(member, resultType);
            }

            return resultType;
        }

        private bool IsCollectionType(Type type, out Type elementType)
        {
            elementType = null;
            if (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type))
            {
                var typeDefinition = type.GetGenericTypeDefinition();
                if (typeDefinition == typeof(IEnumerable<>)
                    || typeDefinition == typeof(ICollection<>)
                    || typeDefinition == typeof(IList<>))
                {
                    elementType = type.GetGenericArguments()[0];
                    return true;
                }
            }

            return false;
        }

        private static DataTypeType AsConstrained(MemberInfo member, DataTypeType resultType)
        {
            var constraints = new Constraints();
            var requiresConstraints = false;

            requiresConstraints |= ParseStringConstraints(member, constraints);
            requiresConstraints |= ParseThresholds(member, constraints);
            requiresConstraints |= ParseUnits(member, constraints);
            requiresConstraints |= ParseContentType(member, constraints);
            requiresConstraints |= ParseSchemaConstraints(member, constraints);
            requiresConstraints |= ParseIdentifierType(member, constraints);

            if (requiresConstraints)
            {
                var constrainedType = new ConstrainedType
                {
                    DataType = resultType,
                    Constraints = constraints
                };
                resultType = new DataTypeType {Item = constrainedType};
            }

            return resultType;
        }

        private static bool ParseUnits(MemberInfo member, Constraints constraints)
        {
            var requiresConstraints = false;
            if (member.GetCustomAttribute<UnitAttribute>() is var unit && unit != null)
            {
                var u = new ConstraintsUnit
                {
                    Label = unit.Label,
                    Offset = (decimal)unit.Offset,
                    Factor = (decimal)unit.Factor
                };
                var components = new List<ConstraintsUnitUnitComponent>();

                AddComponent(ConstraintsUnitUnitComponentSIUnit.Meter, unit.Meter, components);
                AddComponent(ConstraintsUnitUnitComponentSIUnit.Second, unit.Second, components);
                AddComponent(ConstraintsUnitUnitComponentSIUnit.Kilogram, unit.Kilogram, components);
                AddComponent(ConstraintsUnitUnitComponentSIUnit.Ampere, unit.Ampere, components);
                AddComponent(ConstraintsUnitUnitComponentSIUnit.Kelvin, unit.Kelvin, components);
                AddComponent(ConstraintsUnitUnitComponentSIUnit.Mole, unit.Mole, components);
                AddComponent(ConstraintsUnitUnitComponentSIUnit.Candela, unit.Candela, components);
                u.UnitComponent = components.ToArray();
                constraints.Unit = u;
                requiresConstraints = true;
            }

            return requiresConstraints;
        }

        private static bool ParseThresholds(MemberInfo member, Constraints constraints)
        {
            var requiresConstraints = false;
            if (member.GetCustomAttribute<MinimalExclusiveAttribute>() is var minimal && minimal != null)
            {
                constraints.MinimalExclusive = minimal.Threshold.ToString("R");
                requiresConstraints = true;
            }

            if (member.GetCustomAttribute<MaximalInclusiveAttribute>() is var maximal && maximal != null)
            {
                constraints.MaximalInclusive = maximal.Threshold.ToString("R");
                requiresConstraints = true;
            }

            if (member.GetCustomAttribute<MinimalInclusiveAttribute>() is var minimalInclusive &&
                minimalInclusive != null)
            {
                constraints.MinimalExclusive = minimalInclusive.Threshold.ToString("R");
                requiresConstraints = true;
            }

            if (member.GetCustomAttribute<MaximalInclusiveAttribute>() is var maximalInclusive &&
                maximalInclusive != null)
            {
                constraints.MaximalInclusive = maximalInclusive.Threshold.ToString("R");
                requiresConstraints = true;
            }

            return requiresConstraints;
        }

        private static bool ParseStringConstraints(MemberInfo member, Constraints constraints)
        {
            var requiresConstraints = false;
            if (member.GetCustomAttribute<PatternConstraintAttribute>() is var pattern && pattern != null)
            {
                constraints.Pattern = pattern.Pattern;
                requiresConstraints = true;
            }

            if (member.GetCustomAttribute<MaximalLengthAttribute>() is var maximalLength && maximalLength != null)
            {
                constraints.MaximalLength = maximalLength.MaxLength.ToString();
                requiresConstraints = true;
            }

            return requiresConstraints;
        }

        private static bool ParseSchemaConstraints(MemberInfo member, Constraints constraints)
        {
            var requiresConstraints = false;
            if (member.GetCustomAttribute<SchemaAttribute>() is var schema && schema != null)
            {
                constraints.Schema = new ConstraintsSchema()
                {
                    Item = schema.Schema,
                    ItemElementName = Uri.TryCreate(schema.Schema, UriKind.Absolute, out var _)
                        ? ItemChoiceType.Url
                        : ItemChoiceType.Inline,
                    Type = ConvertSchemaType(schema.Type)
                };
                requiresConstraints = true;
            }

            return requiresConstraints;
        }

        private static ConstraintsSchemaType ConvertSchemaType(SchemaType schemaType)
        {
            switch (schemaType)
            {
                case SchemaType.Xml:
                    return ConstraintsSchemaType.Xml;
                case SchemaType.Json:
                    return ConstraintsSchemaType.Json;
                default:
                    throw new ArgumentOutOfRangeException(nameof(schemaType));
            }
        }

        private static bool ParseContentType(MemberInfo member, Constraints constraints)
        {
            var requiresConstraints = false;
            if (member.GetCustomAttribute<ContentTypeAttribute>() is var contentType && contentType != null)
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

        private static bool ParseIdentifierType(MemberInfo member, Constraints constraints)
        {
            var requiresConstraints = false;
            if (member.GetCustomAttribute<SilaIdentifierTypeAttribute>() is var identifierType && identifierType != null)
            {
                constraints.FullyQualifiedIdentifierSpecified = true;
                switch (identifierType.Type)
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
                        throw new ArgumentOutOfRangeException(nameof(identifierType));
                }
                requiresConstraints = true;
            }

            return requiresConstraints;
        }

        private static void AddComponent(ConstraintsUnitUnitComponentSIUnit unit, sbyte exponent,
            ICollection<ConstraintsUnitUnitComponent> components)
        {
            if (exponent != 0)
            {
                components.Add(new ConstraintsUnitUnitComponent()
                {
                    Exponent = exponent.ToString(),
                    SIUnit = unit
                });
            }
        }

        private DataTypeType GenerateEnum(Type type)
        {
            var fields = Enum.GetNames(type);
            return new DataTypeType
            {
                Item = new ConstrainedType
                {
                    DataType = new DataTypeType {Item = BasicType.String},
                    Constraints = new Constraints
                    {
                        Set = fields
                    }
                }
            };
        }

        private DataTypeType GenerateStructure(Type type, Action<Type> typeFoundAction)
        {
            var identifier = type.GetCustomAttribute<SilaIdentifierAttribute>()?.Identifier ?? type.Name;
            var properties = type.GetProperties();
            if (properties.Length == 1 && properties[0].Name == "Value")
            {
                var prop = properties[0];
                return AsConstrained(type,
                    GenerateTypeReference(prop.PropertyType, identifier + "." + prop.Name, prop, true, null));
            }

            return new DataTypeType
            {
                Item = new StructureType
                {
                    Element = GenerateStructureElements(type, typeFoundAction, identifier)
                }
            };
        }

        private SiLAElement[] GenerateStructureElements(Type type, Action<Type> typeFoundAction, string identifier)
        {
            return (from prop in DetermineStructureProperties(type)
                select new SiLAElement
                {
                    DataType = GenerateTypeReference(prop.PropertyType, identifier + "." + prop.Name, prop,
                        true, typeFoundAction),
                    Identifier = prop.GetCustomAttribute<SilaIdentifierAttribute>()?.Identifier ?? prop.Name,
                    DisplayName = prop.GetCustomAttribute<SilaDisplayNameAttribute>()?.DisplayName ??
                                  prop.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ??
                                  prop.Name,
                    Description = prop.GetCustomAttribute<SilaDescriptionAttribute>()?.Description ??
                                  prop.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty
                }).ToArray();
        }

        private IEnumerable<PropertyInfo> DetermineStructureProperties(Type type)
        {
            var constructor = (from cons in type.GetConstructors()
                orderby cons.GetParameters().Length descending
                select cons).FirstOrDefault();
            if (constructor != null)
            {
                return from par in constructor.GetParameters()
                    select type.GetProperty(CodeGenerationHelper.ToPascalCase(par.Name)) ??
                           throw new InvalidOperationException(
                               $"Could not find matching property for constructor parameter {par.Name} of type {type.Name}");
            }

            var createMethod = (from staticMethod in type.GetMethods(BindingFlags.Static | BindingFlags.Public)
                where staticMethod.Name.StartsWith("From")
                select staticMethod).FirstOrDefault();
            if (createMethod != null)
            {
                NameRegistry.RegisterConstructionMethod(type, createMethod);
                return DetermineStructurePropertiesForConstructionMethod(type, createMethod);
            }

            throw new InvalidOperationException(
                $"Type {type.Name} does not have any public constructors and we did not find any suitable static create method. It therefore cannot be used for code generation.");
        }

        private static IEnumerable<PropertyInfo> DetermineStructurePropertiesForConstructionMethod(Type type,
            MethodInfo createMethod)
        {
            IEnumerable<PropertyInfo> propertiesInCorrectOrder;
            if (createMethod.GetParameters().Length == 1)
            {
                var suffix = createMethod.Name.Substring(4);
                var property = type.GetProperties().FirstOrDefault(p => p.Name.EndsWith(suffix));
                propertiesInCorrectOrder = Enumerable.Repeat(property, 1);
            }
            else
            {
                propertiesInCorrectOrder = from par in createMethod.GetParameters()
                    select type.GetProperty(CodeGenerationHelper.ToPascalCase(par.Name)) ??
                           throw new InvalidOperationException(
                               $"Could not find matching property for create method {createMethod.Name} parameter {par.Name} of type {type.Name}");
            }

            return propertiesInCorrectOrder;
        }

        private static DataTypeType AsCollection(DataTypeType dataType)
        {
            return new DataTypeType
            {
                Item = new ListType
                {
                    DataType = dataType
                }
            };
        }

        private (string, string, string) ParseElementData(MemberInfo member, string prefix = null, string suffix = null)
        {
            var identifier = member.GetCustomAttribute<SilaIdentifierAttribute>()?.Identifier;
            if (identifier == null)
            {
                identifier = GetSuggestedMemberIdentifier(member, prefix, suffix);
            }
            else
            {
                NameRegistry?.RegisterRename(member.Name, identifier);
            }

            var displayName = member.GetCustomAttribute<SilaDisplayNameAttribute>()?.DisplayName
                              ?? member.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName
                              ?? identifier;

            var description = member.GetCustomAttribute<SilaDescriptionAttribute>()?.Description
                              ?? member.GetCustomAttribute<DescriptionAttribute>()?.Description
                              ?? string.Empty;

            return (identifier, displayName, description);
        }

        private static string GetSuggestedMemberIdentifier(MemberInfo member, string prefix, string suffix)
        {
            var identifier = member.Name;
            if (suffix != null && identifier.EndsWith(suffix))
            {
                identifier = identifier.Substring(0, identifier.Length - suffix.Length);
            }

            if (prefix != null && identifier.StartsWith(prefix))
            {
                identifier = identifier.Substring(prefix.Length);
            }

            return identifier;
        }

        private (string, string, string) ParseElementData(ParameterInfo parameter)
        {
            var identifier = parameter.GetCustomAttribute<SilaIdentifierAttribute>()?.Identifier ?? parameter.Name;
            if (string.IsNullOrEmpty(identifier)) identifier = "ReturnValue";

            if (char.IsLower(identifier[0]))
            {
                identifier = char.ToUpper(identifier[0]) + identifier.Substring(1);
            }

            var displayName = parameter.GetCustomAttribute<SilaDisplayNameAttribute>()?.DisplayName
                              ?? parameter.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName
                              ?? identifier;

            var description = parameter.GetCustomAttribute<SilaDescriptionAttribute>()?.Description
                              ?? parameter.GetCustomAttribute<DescriptionAttribute>()?.Description;

            return (identifier, displayName, description);
        }

        private FeatureCommand GenerateCommand(MethodInfo methodInfo, ICollection<Type> executeExceptions,
            ICollection<Type> dataTypes)
        {
            var (identifier, displayName, description) = ParseElementData(methodInfo);

            var exceptionIdentifiers = ParseExceptionNamesAndRegister(methodInfo, executeExceptions);
            var parameters = ParseCommandParameters(methodInfo, dataTypes, identifier);
            var (responseType, intermediateType, isObservable) = ParseCommandTypes(methodInfo.ReturnType);

            var inlineResponse = methodInfo.ReturnTypeCustomAttributes.IsDefined(typeof(InlineStructAttribute), false);

            var command = new FeatureCommand
            {
                Identifier = identifier,
                DisplayName = displayName,
                Description = description,
                DefinedExecutionErrors = exceptionIdentifiers.Any() ? exceptionIdentifiers.ToArray() : null,
                Parameter = parameters,
                Response = CreateCommandResponse(identifier, methodInfo.ReturnParameter, responseType, inlineResponse, dataTypes),
                Observable = isObservable ? FeatureCommandObservable.Yes : FeatureCommandObservable.No
            };
            if (intermediateType != null)
            {
                SetCommandIntermediateResponseType(dataTypes, command, intermediateType, identifier);
            }

            return command;
        }

        private void SetCommandIntermediateResponseType(ICollection<Type> dataTypes, FeatureCommand command,
            Type intermediateType,
            string identifier)
        {
            command.IntermediateResponse = new SiLAElement[]
            {
                new SiLAElement()
                {
                    Identifier = "Intermediate",
                    DisplayName = "Intermediate",
                    Description = "",
                    DataType = GenerateTypeReferenceAndRegister(intermediateType, identifier + ".Intermediate", null,
                        dataTypes)
                }
            };
        }

        private SiLAElement[] ParseCommandParameters(MethodInfo methodInfo, ICollection<Type> dataTypes,
            string identifier)
        {
            var parameters = new List<SiLAElement>();
            foreach (var parameterInfo in methodInfo.GetParameters())
            {
                var (parameterId, parameterName, parameterDescription) = ParseElementData(parameterInfo);

                parameters.Add(new SiLAElement
                {
                    Identifier = parameterId,
                    DisplayName = parameterName,
                    Description = parameterDescription ?? string.Empty,
                    DataType = GenerateTypeReferenceAndRegister(parameterInfo.ParameterType,
                        identifier + "." + parameterId,
                        null, dataTypes)
                });
            }

            return parameters.ToArray();
        }

        private SiLAElement[] CreateCommandResponse(string commandId, ParameterInfo returnParameter, Type responseType, bool inlineResponse, ICollection<Type> dataTypes)
        {
            if (responseType == typeof(void))
            {
                return new SiLAElement[0];
            }

            if (inlineResponse && responseType.IsValueType)
            {
                return GenerateStructureElements(responseType, dataTypes.Add, commandId + "." + "Return");
            }
            else
            {
                var (responseId, responseName, responseDescription) = ParseElementData(returnParameter);
                return new[]
                {
                    new SiLAElement()
                    {
                        Identifier = responseId,
                        DisplayName = responseName,
                        Description = responseDescription ?? string.Empty,
                        DataType = GenerateTypeReferenceAndRegister(responseType, commandId + "." + responseId, null,
                            dataTypes)
                    }
                };
            }
        }

        private (Type, Type, bool) ParseCommandTypes(Type returnType)
        {
            if (returnType.IsGenericType)
            {
                var typeDefinition = returnType.GetGenericTypeDefinition();
                if (typeDefinition == typeof(IObservableCommand<>))
                {
                    return (returnType.GetGenericArguments()[0], null, true);
                }

                if (typeDefinition == typeof(IIntermediateObservableCommand<,>))
                {
                    var arguments = returnType.GetGenericArguments();
                    return (arguments[1], arguments[0], true);
                }

                if (typeDefinition == typeof(IIntermediateObservableCommand<>))
                {
                    var arguments = returnType.GetGenericArguments();
                    return (typeof(void), arguments[0], true);
                }
            }
            else if (returnType == typeof(IObservableCommand))
            {
                return (typeof(void), null, true);
            }

            return (returnType, null, false);
        }
    }
}
