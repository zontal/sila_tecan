using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

        private readonly ByteSerializer<DynamicObjectProperty> _responseSerializer;

        /// <summary>
        /// Creates a new client for the given property
        /// </summary>
        /// <param name="property">The property</param>
        /// <param name="context">The context in which the property is called</param>
        public PropertyClient(FeatureProperty property, FeatureContext context)
        {
            Property = property;
            Context = context;
            _responseSerializer =
                new ByteSerializer<DynamicObjectProperty>(SerializeRequest, DeserializeResponse);
        }

        private byte[] SerializeRequest( DynamicObjectProperty value )
        {
            return Context.Serializer.Serialize( value, null );
        }

        /// <summary>
        /// Requests the value for the property one time
        /// </summary>
        /// <param name="additionalMetadata">A dictionary with additional request metadata</param>
        /// <returns>A dynamic property with the property value</returns>
        public DynamicObjectProperty RequestValue( IDictionary<string, byte[]> additionalMetadata = null )
        {
            var callInfo = MetadataHelper.TakeInMetadata( Context.ExecutionManager.CreateCallOptions( PropertyIdentifier ), additionalMetadata );

            DynamicObjectProperty result = null;


            try
            {
                if( Property.Observable == FeaturePropertyObservable.Yes )
                {
                    var cts = new CancellationTokenSource();

                    void SetResult( DynamicObjectProperty res )
                    {
                        result = res;
                        cts.Cancel();
                    }

                    Context.Channel.SubscribeProperty( Context.ServiceName, Property.Identifier, Context.Feature.GetFullyQualifiedIdentifier(Property), SetResult, callInfo, _responseSerializer, cts.Token ).Wait();
                }
                else
                {
                    result = Context.Channel.ReadProperty( Context.ServiceName, Property.Identifier, Context.Feature.GetFullyQualifiedIdentifier( Property ), callInfo, _responseSerializer );
                    callInfo.FinishSuccessful();
                }
            }
            catch( Exception e )
            {
                var exception = Context.ConvertException( e );
                // OperationCancelledException is because we cancelled the subscription
                if( !(exception is OperationCanceledException) )
                {
                    callInfo.FinishWithErrors( exception );
                    throw exception;
                }
            }

            return result;
        }

        /// <summary>
        /// Subscribes to the property
        /// </summary>
        /// <param name="callback">The callback to which the updated values should be returned to</param>
        /// <param name="cancellationToken">A cancellation token to cancel the subscription</param>
        /// <param name="additionalMetadata">Additional metadata</param>
        /// <returns>A task that represents the subscription</returns>
        public Task Subscribe( Action<DynamicObjectProperty> callback, CancellationToken cancellationToken = default, IDictionary<string, byte[]> additionalMetadata = null )
        {
            if( Property.Observable != FeaturePropertyObservable.Yes )
            {
                throw new NotSupportedException( "The property is not observable" );
            }

            var callInfo = MetadataHelper.TakeInMetadata( Context.ExecutionManager.CreateCallOptions( PropertyIdentifier ), additionalMetadata );
            try
            {
                var subscription = Context.Channel.SubscribeProperty( Context.ServiceName, Property.Identifier, Context.Feature.GetFullyQualifiedIdentifier( Property ), callback, callInfo, _responseSerializer, cancellationToken );
                
                callInfo.FinishSuccessful();
                return subscription;
            }
            catch(Exception e)
            {
                var exception = Context.ConvertException( e );
                callInfo.FinishWithErrors( exception );
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
