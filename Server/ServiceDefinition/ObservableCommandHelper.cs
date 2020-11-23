using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Grpc.Core;
using Tecan.Sila2.Client;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Helper class for observable commands
    /// </summary>
    public static class ObservableCommandHelper
    {
        /// <summary>
        /// Subscribe to an observable property
        /// </summary>
        /// <typeparam name="T">The type of the data</typeparam>
        /// <param name="callInvoker">An object that performs the calls</param>
        /// <param name="subscribeMethod">The method to subscribe to values</param>
        /// <param name="newValueReceived">A method that is called when new data arrives</param>
        /// <param name="callInfo">Caller information for the property subscription</param>
        /// <returns>A task that ends when the subscription ends.</returns>
        public static Task SubscribeTo<T>( CallInvoker callInvoker, Method<EmptyRequest, T> subscribeMethod, Action<T> newValueReceived, IClientCallInfo callInfo )
            where T : class
        {
            var asyncCall = callInvoker.AsyncServerStreamingCall( subscribeMethod, null,
                callInfo.ToCallOptions(), EmptyRequest.Instance );
            var responseStream = asyncCall.ResponseStream;
            if( responseStream.MoveNext(default).Result )
            {
                newValueReceived?.Invoke( responseStream.Current );
                return ContinueReading( responseStream, newValueReceived, callInfo );
            }

            return Task.CompletedTask;
        }

        private static async Task ContinueReading<T>( IAsyncStreamReader<T> responseStream, Action<T> newValueReceived, IClientCallInfo callInfo ) where T : class
        {
            try
            {
                while(await responseStream.MoveNext(default))
                {
                    newValueReceived?.Invoke( responseStream.Current );
                }
                callInfo.FinishSuccessful();
            }
            catch( Exception exception )
            {
                callInfo.FinishWithErrors(exception);
                throw;
            }
        }
    }
}
