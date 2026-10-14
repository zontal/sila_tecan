using NUnit.Framework;
using ProtoBuf.Meta;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Tecan.Sila2.Locking.LockController;

namespace Tecan.Sila2.IntegrationTests
{
    /// <summary>
    /// Verifies that the compile-time generated protobuf-net type models produce the same wire format as the reflection-based runtime model
    /// </summary>
    [TestFixture]
    public class SerializationModelFixture
    {
        private const int MaxDepth = 3;

        public static IEnumerable<TestCaseData> FrameworkContracts => GetContracts( typeof( FrameworkTypeModel ) );

        public static IEnumerable<TestCaseData> GeneratedFeatureContracts =>
            new[]
            {
                typeof( LockControllerTypeModel ),
                typeof( Authentication.AuthenticationService.AuthenticationServiceTypeModel ),
                typeof( Cancellation.CancelController.CancelControllerTypeModel ),
                typeof( ConnectionConfiguration.ConnectionConfigurationService.ConnectionConfigurationServiceTypeModel ),
                typeof( TestServer.TestServerTypeModel ),
            }.SelectMany( GetContracts );

        [TestCaseSource( nameof( FrameworkContracts ) )]
        [TestCaseSource( nameof( GeneratedFeatureContracts ) )]
        public void GeneratedModel_MatchesRuntimeModel( Type modelType, Type contract )
        {
            var model = GetInstance( modelType );
            var instance = Populate( contract, 0 );

            var runtimeBytes = Serialize( contract, instance, RuntimeTypeModel.Create() );
            var generatedBytes = Serialize( contract, instance, model );
            if(contract.GetProperties().Any( property => property.CanWrite ))
            {
                Assert.That( runtimeBytes, Is.Not.Empty, "the test instance should carry data" );
            }
            Assert.That( generatedBytes, Is.EqualTo( runtimeBytes ) );

            var roundTripped = Deserialize( contract, generatedBytes, model );
            Assert.That( Serialize( contract, roundTripped, model ), Is.EqualTo( generatedBytes ) );
        }

        [Test]
        public void FrameworkModel_SerializesClosedGenericsUsedByFramework()
        {
            Assert.That( FrameworkTypeModel.Instance.CanSerialize( typeof( PropertyResponse<StringDto> ) ), Is.True );
            Assert.That( FrameworkTypeModel.Instance.CanSerialize( typeof( PropertyResponse<List<StringDto>> ) ), Is.True );
        }

        [Test]
        public void AffectedCallsMarshaller_IsWireCompatibleWithRepeatedStringMessage()
        {
            var calls = new List<StringDto> { new StringDto( "a/b/v1/Command/C" ), new StringDto( "a/b/v1/Property/P" ) };
            var runtimeModel = RuntimeTypeModel.Create();

            var bytes = FrameworkMarshallers.AffectedCalls.Serializer( calls );
            Assert.That( bytes, Is.EqualTo( ByteSerializer.ToByteArray( calls, runtimeModel ) ) );

            var deserialized = FrameworkMarshallers.AffectedCalls.Deserializer( bytes );
            Assert.That( deserialized.Select( dto => dto.Value ), Is.EqualTo( calls.Select( dto => dto.Value ) ) );
            Assert.That( FrameworkMarshallers.AffectedCalls.Deserializer( new byte[0] ), Is.Empty );
        }

        [Test]
        public void GeneratedSerializer_RoundTripsFeatureDto()
        {
            var serializer = LockControllerSerializers.LockServerRequestDto;
            var request = new LockServerRequestDto( "lock", 42, null );

            var deserialized = serializer.Deserializer( serializer.Serializer( request ) );

            Assert.That( deserialized.LockIdentifier.Value, Is.EqualTo( "lock" ) );
            Assert.That( deserialized.Timeout.Value, Is.EqualTo( 42 ) );
        }

        [Test]
        public void Create_RejectsMissingModel()
        {
            Assert.That( () => ByteSerializer.Create<StringDto>( null ), Throws.ArgumentNullException );
            Assert.That( () => ByteSerializer.ToByteArray( new StringDto( "x" ), null ), Throws.ArgumentNullException );
            Assert.That( () => ByteSerializer.FromByteArray<StringDto>( new byte[0], null ), Throws.ArgumentNullException );
        }

        private static IEnumerable<TestCaseData> GetContracts( Type modelType )
        {
            return modelType.GetCustomAttributesData()
                .Where( attribute => attribute.AttributeType.Name == "ProtoSerializableAttribute" )
                .Select( attribute => (Type)attribute.ConstructorArguments[0].Value )
                .Select( contract => new TestCaseData( modelType, contract ).SetArgDisplayNames( modelType.Name, contract.ToString() ) );
        }

        private static TypeModel GetInstance( Type modelType )
        {
            return (TypeModel)modelType.GetProperty( "Instance", BindingFlags.Public | BindingFlags.Static ).GetValue( null );
        }

        private static byte[] Serialize( Type contract, object instance, TypeModel model )
        {
            return (byte[])Invoke( nameof( ByteSerializer.ToByteArray ), contract, instance, model );
        }

        private static object Deserialize( Type contract, byte[] data, TypeModel model )
        {
            return Invoke( nameof( ByteSerializer.FromByteArray ), contract, data, model );
        }

        private static object Invoke( string methodName, Type contract, object argument, TypeModel model )
        {
            var method = typeof( ByteSerializer ).GetMethods()
                .Single( m => m.Name == methodName && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType == typeof( TypeModel ) );
            try
            {
                return method.MakeGenericMethod( contract ).Invoke( null, new[] { argument, model } );
            }
            catch(TargetInvocationException exception)
            {
                throw exception.InnerException;
            }
        }

        private static object Populate( Type type, int depth )
        {
            var value = CreateValue( type, depth );
            return value ?? Activator.CreateInstance( type );
        }

        private static object CreateValue( Type type, int depth )
        {
            var nullable = Nullable.GetUnderlyingType( type );
            if(nullable != null)
            {
                return CreateValue( nullable, depth );
            }
            if(type == typeof( string ))
            {
                return "value" + depth;
            }
            if(type == typeof( bool ))
            {
                return true;
            }
            if(type == typeof( byte[] ))
            {
                return new byte[] { 1, 2, 3 };
            }
            if(type == typeof( Guid ))
            {
                return new Guid( "0f8fad5b-d9cb-469f-a165-70867728950e" );
            }
            if(type.IsEnum)
            {
                var values = Enum.GetValues( type );
                return values.GetValue( values.Length - 1 );
            }
            if(type.IsPrimitive || type == typeof( decimal ))
            {
                return Convert.ChangeType( 7, type );
            }
            if(depth > MaxDepth || type.IsAbstract || type.IsInterface || type == typeof( object ))
            {
                return null;
            }
            var listType = GetListElementType( type );
            if(listType != null)
            {
                var list = (IList)Activator.CreateInstance( type );
                FillList( list, listType, depth );
                return list;
            }
            if(type.GetConstructor( Type.EmptyTypes ) == null)
            {
                return null;
            }
            var instance = Activator.CreateInstance( type );
            foreach(var property in type.GetProperties( BindingFlags.Public | BindingFlags.Instance ))
            {
                if(property.GetIndexParameters().Length > 0)
                {
                    continue;
                }
                var elementType = GetListElementType( property.PropertyType );
                if(!property.CanWrite && elementType != null && property.GetValue( instance ) is IList existing)
                {
                    FillList( existing, elementType, depth + 1 );
                }
                else if(property.CanWrite && property.GetSetMethod() != null)
                {
                    var value = CreateValue( property.PropertyType, depth + 1 );
                    if(value != null)
                    {
                        property.SetValue( instance, value );
                    }
                }
            }
            return instance;
        }

        private static void FillList( IList list, Type elementType, int depth )
        {
            for(var i = 0; i < 2; i++)
            {
                var element = CreateValue( elementType, depth + 1 );
                if(element != null)
                {
                    list.Add( element );
                }
            }
        }

        private static Type GetListElementType( Type type )
        {
            if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof( List<> ))
            {
                return type.GetGenericArguments()[0];
            }
            return null;
        }
    }
}
