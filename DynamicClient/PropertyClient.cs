using System;
using System.Threading.Tasks;
using Grpc.Core;
using Tecan.Sila2.Client;

namespace Tecan.Sila2.DynamicClient
{
    /// <summary>
    /// Denotes a client for a SiLA2 property
    /// </summary>
    public class PropertyClient
    {
        /// <summary>
        /// Gets the property that is targeted
        /// </summary>
        public FeatureProperty Property { get; }

        /// <summary>
        /// Gets the context in which the property is called
        /// </summary>
        public FeatureContext Context { get; }

        /// <summary>
        /// Gets the fully qualified identifier of the property
        /// </summary>
        public string PropertyIdentifier => Context.Feature.GetFullyQualifiedIdentifier(Property);

        private readonly Method<EmptyRequest, DynamicObjectProperty> _method;

        /// <summary>
        /// Creates a new client for the given property
        /// </summary>
        /// <param name="property">The property</param>
        /// <param name="context">The context in which the property is called</param>
        public PropertyClient(FeatureProperty property, FeatureContext context)
        {
            Property = property;
            Context = context;
            var responseMarshaller =
                new Marshaller<DynamicObjectProperty>(SerializeRequest, DeserializeResponse);
            if (property.Observable == FeaturePropertyObservable.Yes)
            {
                _method = new Method<EmptyRequest, DynamicObjectProperty>(MethodType.ServerStreaming,
                    context.ServiceName,
                    "Subscribe_" + property.Identifier, EmptyRequest.Marshaller, responseMarshaller);
            }
            else
            {
                _method = new Method<EmptyRequest, DynamicObjectProperty>(MethodType.Unary, context.ServiceName,
                    "Get_" + property.Identifier, EmptyRequest.Marshaller, responseMarshaller);

            }
        }

        private byte[] SerializeRequest( DynamicObjectProperty value )
        {
            return Context.Serializer.Serialize( value, null );
        }

        private async Task<DynamicObjectProperty> RequestDynamic( IClientCallInfo callInfo )
        {
            var asyncCall =
                Context.CallInvoker.AsyncServerStreamingCall( _method, null, callInfo.Options, EmptyRequest.Instance );
            var responseStream = asyncCall.ResponseStream;
            if( await responseStream.MoveNext() )
            {
                return responseStream.Current;
            }

            throw new InvalidOperationException("Property could not be read.");
        }

        /// <summary>
        /// Requests the value for the property one time
        /// </summary>
        /// <param name="host">The host at which the property value should be queries or null to use the default host</param>
        /// <returns>A dynamic property with the property value</returns>
        public DynamicObjectProperty RequestValue(string host = null)
        {
            var callInfo = Context.ExecutionManager.CreateCallOptions(PropertyIdentifier);
            try
            {
                DynamicObjectProperty result;
                if (Property.Observable == FeaturePropertyObservable.Yes)
                {
                    var task = RequestDynamic(callInfo);
                    task.Wait();
                    result = task.Result;
                }
                else
                {
                    result = Context.CallInvoker.BlockingUnaryCall(
                        _method,
                        host,
                        callInfo.Options,
                        EmptyRequest.Instance);
                }
                callInfo.FinishSuccessful();
                return result;
            }
            catch (Exception e)
            {
                var exception = Context.ConvertException(e);
                callInfo.FinishWithErrors(exception);
                throw exception;
            }
        }

        private DynamicObjectProperty DeserializeResponse(byte[] data)
        {
            try
            {
                var property = new DynamicObjectProperty(Property.Identifier, Property.DisplayName, Property.Description, Property.DataType);
                Context.Serializer.Deserialize(property, data, true, Context.ExecutionManager.DownloadBinaryStore);
                return property;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
