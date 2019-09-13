using System;
using System.Threading.Tasks;
using Grpc.Core;
using Tecan.Sila2.Client;

namespace Tecan.Sila2.DynamicClient
{
    public class PropertyClient
    {
        public FeatureProperty Property { get; }

        public FeatureContext Context { get; }

        public string PropertyIdentifier => Context.Feature.GetFullyQualifiedIdentifier(Property);

        private readonly Method<EmptyRequest, DynamicObjectProperty> _method;


        public PropertyClient(FeatureProperty property, FeatureContext context)
        {
            Property = property;
            Context = context;
            var responseMarshaller =
                new Marshaller<DynamicObjectProperty>(Context.Serializer.Serialize, DeserializeResponse);
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
                Context.Serializer.Deserialize(property, data, true);
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
