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
    [Export( typeof( ICodeNameProvider ) )]
    [Export( typeof( ICodeNameRegistry ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class CodeNameProvider : ICodeNameProvider, ICodeNameRegistry
    {
        private readonly Dictionary<string, string> _renames = new Dictionary<string, string>();
        private readonly Dictionary<string, MethodInfo> _constructorReplacements = new Dictionary<string, MethodInfo>();
        private readonly Dictionary<string, MethodInfo> _nonstandardMethods = new Dictionary<string, MethodInfo>();
        private readonly Dictionary<string, Type> _typeOverrides = new Dictionary<string, Type>();

        /// <inheritdoc />
        public string GetStaticConstructorMethod( string typeName )
        {
            if(_constructorReplacements.TryGetValue( typeName, out var method ))
            {
                return method.Name;
            }
            else
            {
                return null;
            }
        }

        /// <inheritdoc />
        public void RegisterConstructionMethod( Type type, MethodInfo constructorMethod )
        {
            _constructorReplacements[type.Name] = constructorMethod;
        }

        /// <inheritdoc />
        public CodeTypeReference CreateFeatureInterfaceReference( Feature feature )
        {
            if(_renames.TryGetValue( feature.Identifier, out var identifier )) return new CodeTypeReference( identifier );
            return new CodeTypeReference( "I" + feature.Identifier );
        }

        /// <inheritdoc />
        public void RegisterRename( string memberName, string identifier )
        {
            _renames[identifier] = memberName;
        }

        /// <inheritdoc />
        public string GetCommandName( FeatureCommand featureCommand )
        {
            if(_renames.TryGetValue( featureCommand.Identifier, out var identifier )) return identifier;
            return featureCommand.Identifier;
        }

        /// <inheritdoc />
        public string GetPropertyName( FeatureProperty featureProperty )
        {
            if(_renames.TryGetValue( featureProperty.Identifier, out var identifier )) return identifier;
            return featureProperty.Identifier;
        }

        /// <inheritdoc />
        public CodeTypeReference CreateExceptionReference( string errorIdentifier )
        {
            if(_renames.TryGetValue( errorIdentifier, out var identifier )) return new CodeTypeReference( identifier );
            return new CodeTypeReference( errorIdentifier + "Exception" );
        }

        /// <inheritdoc />
        public void RegisterDifferentType( string identifier, Type type )
        {
            _typeOverrides[ identifier ] = type;
        }

        public void RegisterMethod( string commandIdentifier, MethodInfo method )
        {
            _nonstandardMethods[ commandIdentifier ] = method;
        }

        /// <inheritdoc />
        public CodeTypeReference GetMemberType( string identifier )
        {
            if(_typeOverrides.TryGetValue( identifier, out var type ))
            {
                return new CodeTypeReference( type );
            }
            else
            {
                return null;
            }
        }

        public CodeTypeReference GenerateCommandRequestType( FeatureCommand command )
        {
            return new CodeTypeReference( command.Identifier + "RequestDto" );
        }

        public CodeTypeReference GenerateCommandIntermediateType( FeatureCommand command )
        {
            if(command.IntermediateResponse == null || command.IntermediateResponse.Length == 0)
            {
                return new CodeTypeReference( nameof( EmptyRequest ) );
            }
            else
            {
                return new CodeTypeReference( command.Identifier + "IntermediateDto" );
            }
        }

        public CodeTypeReference GenerateCommandResponseType( FeatureCommand command )
        {
            if(command.Response == null || command.Response.Length == 0)
            {
                return new CodeTypeReference( nameof( EmptyRequest ) );
            }
            else
            {
                return new CodeTypeReference( command.Identifier + "ResponseDto" );
            }
        }

        public CodeTypeReference GetPropertyResponseType( FeatureProperty featureProperty, ITypeTranslationProvider typeTranslationProvider )
        {
            CodeTypeReference propertyResponse;
            if(featureProperty.DataType.Item is ConstrainedType)
            {
                propertyResponse = new CodeTypeReference( featureProperty.Identifier + "ResponseDto" );
            }
            else
            {
                propertyResponse = new CodeTypeReference( typeof( PropertyResponse<> ).Name,
                    typeTranslationProvider.GetDtoTypeReference( featureProperty.DataType, featureProperty.Identifier, null ) );
            }

            return propertyResponse;
        }

        public CodeTypeReference GetObservableCommandReturnType( FeatureCommand featureCommand, ITypeTranslationProvider typeTranslationProvider )
        {
            CodeTypeReference intermediateType = GetIntermediateType( featureCommand, typeTranslationProvider );
            if(featureCommand.Response != null && featureCommand.Response.Length > 0)
            {
                var responseType = GetMemberType( featureCommand.Identifier + "." + featureCommand.Response[0].Identifier )
                                   ?? typeTranslationProvider.ExtractType( featureCommand.Response[0].DataType, featureCommand.Identifier );
                if(intermediateType != null)
                {
                    return new CodeTypeReference( typeof( IIntermediateObservableCommand<,> ).FullName, intermediateType, responseType );
                }

                return new CodeTypeReference( typeof( IObservableCommand<> ).FullName, responseType );
            }

            if(intermediateType != null)
            {
                return new CodeTypeReference( typeof( IIntermediateObservableCommand<> ).FullName, intermediateType );
            }

            return new CodeTypeReference( typeof( IObservableCommand ) );
        }


        public CodeTypeReference GetIntermediateType( FeatureCommand featureCommand, ITypeTranslationProvider typeTranslationProvider )
        {
            if(featureCommand.IntermediateResponse != null && featureCommand.IntermediateResponse.Length > 0)
            {
                return GetMemberType( featureCommand.Identifier + "." + featureCommand.IntermediateResponse[0].Identifier )
                                ?? typeTranslationProvider.ExtractType( featureCommand.IntermediateResponse[0].DataType, featureCommand.Identifier + "Intermediate" );
            }
            return null;
        }

        public MethodInfo GetNonstandardMethod( FeatureCommand command )
        {
            if(_nonstandardMethods.TryGetValue( command.Identifier, out var method ))
            {
                return method;
            }

            return null;
        }

        public MethodInfo GetNonstandardMethod( FeatureProperty property )
        {
            if(_nonstandardMethods.TryGetValue( property.Identifier, out var method ))
            {
                return method;
            }

            return null;
        }

        public bool IsDifferentTypeRegistered( string identifier )
        {
            return _typeOverrides.ContainsKey( identifier );
        }
    }
}
