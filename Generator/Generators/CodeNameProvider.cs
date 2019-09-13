using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Reflection;
using Tecan.Sila2.Generator.Contracts;

namespace Tecan.Sila2.Generator.Generators
{
    /// <summary>
    /// A class that registers renames
    /// </summary>
    [Export(typeof(ICodeNameProvider))]
    [Export(typeof(ICodeNameRegistry))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class CodeNameProvider : ICodeNameProvider, ICodeNameRegistry
    {
        private readonly Dictionary<string, string> _renames = new Dictionary<string, string>();
        private readonly Dictionary<string, MethodInfo> _constructorReplacements = new Dictionary<string, MethodInfo>();
        private readonly Dictionary<string, Type> _typeOverrides = new Dictionary<string, Type>();

        /// <inheritdoc />
        public string GetStaticConstructorMethod(string typeName)
        {
            if (_constructorReplacements.TryGetValue(typeName, out var method))
            {
                return method.Name;
            }
            else
            {
                return null;
            }
        }

        /// <inheritdoc />
        public void RegisterConstructionMethod(Type type, MethodInfo constructorMethod)
        {
            _constructorReplacements.Add(type.Name, constructorMethod);
        }

        /// <inheritdoc />
        public CodeTypeReference CreateFeatureInterfaceReference(Feature feature)
        {
            if (_renames.TryGetValue(feature.Identifier, out var identifier)) return new CodeTypeReference(identifier);
            return new CodeTypeReference("I" + feature.Identifier);
        }

        /// <inheritdoc />
        public void RegisterRename(string memberName, string identifier)
        {
            _renames.Add(identifier, memberName);
        }

        /// <inheritdoc />
        public string GetCommandName(FeatureCommand featureCommand)
        {
            if (_renames.TryGetValue(featureCommand.Identifier, out var identifier)) return identifier;
            return featureCommand.Identifier;
        }

        /// <inheritdoc />
        public string GetPropertyName(FeatureProperty featureProperty)
        {
            if (_renames.TryGetValue(featureProperty.Identifier, out var identifier)) return identifier;
            return featureProperty.Identifier;
        }

        /// <inheritdoc />
        public CodeTypeReference CreateExceptionReference(string errorIdentifier)
        {
            if (_renames.TryGetValue(errorIdentifier, out var identifier)) return new CodeTypeReference(identifier);
            return new CodeTypeReference(errorIdentifier + "Exception");
        }

        /// <inheritdoc />
        public void RegisterDifferentType(string identifier, Type type)
        {
            _typeOverrides.Add(identifier, type);
        }

        /// <inheritdoc />
        public CodeTypeReference GetMemberType(string identifier)
        {
            if (_typeOverrides.TryGetValue(identifier, out var type))
            {
                return new CodeTypeReference(type);
            }
            else
            {
                return null;
            }
        }

        public CodeTypeReference GenerateCommandRequestType(FeatureCommand command)
        {
            return new CodeTypeReference(command.Identifier + "RequestDto");
        }

        public CodeTypeReference GenerateCommandIntermediateType(FeatureCommand command)
        {
            if (command.IntermediateResponse == null || command.IntermediateResponse.Length == 0)
            {
                return new CodeTypeReference(nameof(EmptyRequest));
            }
            else
            {
                return new CodeTypeReference(command.Identifier + "IntermediateDto");
            }
        }

        public CodeTypeReference GenerateCommandResponseType(FeatureCommand command)
        {
            if (command.Response == null || command.Response.Length == 0)
            {
                return new CodeTypeReference(nameof(EmptyRequest));
            }
            else
            {
                return new CodeTypeReference(command.Identifier + "ResponseDto");
            }
        }

        public CodeTypeReference GetPropertyResponseType(FeatureProperty featureProperty)
        {
            CodeTypeReference propertyResponse;
            if (featureProperty.DataType.Item is ConstrainedType)
            {
                propertyResponse = new CodeTypeReference(featureProperty.Identifier + "ResponseDto");
            }
            else
            {
                propertyResponse = new CodeTypeReference(typeof(PropertyResponse<>).Name,
                    DtoGenerator.GetDtoTypeReference(featureProperty.DataType));
            }

            return propertyResponse;
        }
    }
}
