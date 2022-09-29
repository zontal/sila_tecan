using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using Tecan.Sila2.DynamicClient;
using Tecan.Sila2.Generator.Contracts;

namespace Tecan.Sila2.Generator.TypeTranslation
{
    [Export( typeof( ITypeTranslator ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class BasicTypeTranslator : ITypeTranslator
    {
        private readonly ICodeNameRegistry _nameRegistry;

        [ImportingConstructor]
        public BasicTypeTranslator( ICodeNameRegistry nameRegistry )
        {
            _nameRegistry = nameRegistry;
        }

        private static readonly Dictionary<Type, (BasicType Type, bool IsNonDefault)> BasicTypes = new Dictionary<Type, (BasicType, bool)>
        {
            { typeof(string), (BasicType.String, false) },
            { typeof(byte), (BasicType.Integer, true) },
            { typeof(sbyte), (BasicType.Integer, true) },
            { typeof(ushort), (BasicType.Integer, true) },
            { typeof(short), (BasicType.Integer, true) },
            { typeof(uint), (BasicType.Integer, true) },
            { typeof(int), (BasicType.Integer, true) },
            { typeof(ulong), (BasicType.Integer, true) },
            { typeof(long), (BasicType.Integer, false) },
            { typeof(float), (BasicType.Real, true) },
            { typeof(double), (BasicType.Real, false) },
            { typeof(bool), (BasicType.Boolean, false) },
            { typeof(byte[]), (BasicType.Binary, true) },
            { typeof(DateTimeOffset), (BasicType.Timestamp, false) },
            { typeof(DateTime), (BasicType.Timestamp, true) },
            { typeof(TimeSpan), (BasicType.Time, false) },
            { typeof(FileInfo), (BasicType.Binary, true) },
            { typeof(Stream), (BasicType.Binary, false) },
            { typeof(DynamicObjectProperty), (BasicType.Any, false) },
            { typeof(byte?), (BasicType.Integer, true) },
            { typeof(sbyte?), (BasicType.Integer, true) },
            { typeof(ushort?), (BasicType.Integer, true) },
            { typeof(short?), (BasicType.Integer, true) },
            { typeof(uint?), (BasicType.Integer, true) },
            { typeof(int?), (BasicType.Integer, true) },
            { typeof(ulong?), (BasicType.Integer, true) },
            { typeof(long?), (BasicType.Integer, true) },
            { typeof(float?), (BasicType.Real, true) },
            { typeof(double?), (BasicType.Real, true) },
            { typeof(bool?), (BasicType.Boolean, true) },
            { typeof(object), (BasicType.Any, false) }
        };

        public int Priority => 1;

        public bool TryTranslate( ITypeTranslationProvider translationProvider, Type interfaceType, string origin, out DataTypeType silaType )
        {
            if(BasicTypes.TryGetValue( interfaceType, out var tuple ))
            {
                if(tuple.IsNonDefault)
                {
                    _nameRegistry?.RegisterDifferentType( origin, interfaceType );
                }

                silaType = new DataTypeType()
                {
                    Item = tuple.Type
                };
                return true;
            }

            silaType = null;
            return false;
        }

        public bool TraverseTypes( ITypeTranslationProvider translationProvider, Type interfaceType, string origin, Action<Type> typeAction )
        {
            return BasicTypes.ContainsKey( interfaceType );
        }

        public bool TryTranslate( ITypeTranslationProvider translationProvider, DataTypeType silaType, string suggestedName, out ITypeTranslationInfo translationInfo, Action<Constraints> constraintHandler = null, Action<string, StructureType> structHandler = null )
        {
            switch(silaType.Item)
            {
                case BasicType basic:
                    switch(basic)
                    {
                        case BasicType.Boolean:
                            translationInfo = new TranslationInfo( typeof( bool ), new CodeTypeReference( typeof( BooleanDto ) ) );
                            return true;
                        case BasicType.Date:
                            translationInfo = new TranslationInfo( typeof( DateTimeOffset ), new CodeTypeReference( typeof( DateDto ) ) );
                            return true;
                        case BasicType.Timestamp:
                            translationInfo = new TranslationInfo( typeof( DateTimeOffset ), new CodeTypeReference( typeof( TimestampDto ) ) );
                            return true;
                        case BasicType.Time:
                            translationInfo = new TranslationInfo( typeof( TimeSpan ), new CodeTypeReference( typeof( TimeDto ) ) );
                            return true;
                        case BasicType.Integer:
                            translationInfo = new TranslationInfo( typeof( long ), new CodeTypeReference( typeof( IntegerDto ) ) );
                            return true;
                        case BasicType.Binary:
                            translationInfo = new TranslationInfo( typeof( Stream ), new CodeTypeReference( typeof( BinaryDto ) ) );
                            return true;
                        case BasicType.String:
                            translationInfo = new TranslationInfo( typeof( string ), new CodeTypeReference( typeof( StringDto ) ) );
                            return true;
                        case BasicType.Real:
                            translationInfo = new TranslationInfo( typeof( double ), new CodeTypeReference( typeof( RealDto ) ) );
                            return true;
                        case BasicType.Any:
                            translationInfo = new TranslationInfo( typeof( DynamicObjectProperty ), new CodeTypeReference( typeof( AnyTypeDto ) ) );
                            return true;
                    }

                    break;
            }

            translationInfo = null;
            return false;
        }

        private class TranslationInfo : ITypeTranslationInfo
        {
            private readonly Type _interfaceType;

            public TranslationInfo( Type interfaceType, CodeTypeReference dataTransferType )
            {
                _interfaceType = interfaceType;
                DataTransferType = dataTransferType;
            }

            public CodeTypeReference InterfaceType => new CodeTypeReference( _interfaceType );

            public CodeTypeReference DataTransferType
            {
                get;
            }

            public CodeExpression Encapsulate( CodeExpression expression, CodeExpression binaryStorageArgument )
            {
                return new CodeObjectCreateExpression( DataTransferType, expression, binaryStorageArgument );
            }

            public CodeExpression Extract( CodeExpression expression, CodeTypeReference targetType, CodeExpression binaryStorageArgument )
            {
                var extractedResult = new CodeMethodInvokeExpression( expression, nameof( ISilaTransferObject<string>.Extract ), binaryStorageArgument );
                if(BasicTypes.TryGetValue( _interfaceType, out var tuple ))
                {
                    if(targetType == null)
                    {
                        return extractedResult;
                    }

                    if(tuple.Type == BasicType.Binary)
                    {
                        if(targetType.ArrayRank == 1)
                        {
                            extractedResult.Method.MethodName = nameof( BinaryDto.ExtractToBytes );
                        }
                        else if(targetType.BaseType == typeof( FileInfo ).FullName)
                        {
                            extractedResult.Method.MethodName = nameof( BinaryDto.ExtractToFileInfo );
                        }

                        return extractedResult;
                    }

                    return new CodeCastExpression( targetType, extractedResult );
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            public CodeExpression Extract( CodeExpression expression, CodeTypeReference targetType, CodeExpression binaryStorageArgument, string parameterName )
            {
                var extractedResult = new CodeMethodInvokeExpression( expression, nameof( DtoExtensions.TryExtract ), binaryStorageArgument, new CodePrimitiveExpression( parameterName ) );
                if(BasicTypes.TryGetValue( _interfaceType, out var tuple ))
                {
                    if(targetType == null)
                    {
                        return extractedResult;
                    }

                    if(tuple.Type == BasicType.Binary)
                    {
                        if(targetType.ArrayRank == 1)
                        {
                            extractedResult.Method.MethodName = nameof( BinaryDto.TryExtractToBytes );
                        }
                        else if(targetType.BaseType == typeof( FileInfo ).FullName)
                        {
                            extractedResult.Method.MethodName = nameof( BinaryDto.TryExtractToFileInfo );
                        }

                        return extractedResult;
                    }

                    return new CodeCastExpression( targetType, extractedResult );
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }
    }
}
