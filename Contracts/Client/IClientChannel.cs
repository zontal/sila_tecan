using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Tecan.Sila2.Client
{
    public interface IClientChannel
    {
        T ReadProperty<T>( string serviceName, string propertyName, IClientCallInfo callInfo );

        Task SubscribeProperty<T>( string serviceName, string propertyName, Action<T> newValueReceived, IClientCallInfo callInfo );

        void ExecuteUnobservableCommand<TRequest>( string serviceName, string commandName, TRequest request, IClientCallInfo callInfo );

        TResponse ExecuteUnobservableCommand<TRequest, TResponse>( string serviceName, string commandName, TRequest request, IClientCallInfo callInfo );

        IObservableCommand ExecuteObservableCommand<TRequest>( string serviceName, string commandName, TRequest request, Func<string, string, Exception> errorConverter, IClientCallInfo callInfo );

        IObservableCommand<TResponse> ExecuteObservableCommand<TRequest, TResponse, TResponseDto>( string serviceName, string commandName, TRequest request, Func<TResponseDto, TResponse> responseConverter, Func<string, string, Exception> errorConverter, IClientCallInfo callInfo );

        IIntermediateObservableCommand<TIntermediate> ExecuteIntermediatesCommand<TRequest, TIntermediate, TIntermediateDto>( string serviceName, string commandName, TRequest request, Func<string, string, Exception> errorConverter, IClientCallInfo callInfo );

        IIntermediateObservableCommand<TIntermediate, TResponse> ExecuteIntermediatesCommand<TRequest, TIntermediate, TIntermediateDto, TResponse, TResponseDto>( string serviceName, string commandName, TRequest request, Func<TResponseDto, TResponse> responseConverter, Func<string, string, Exception> errorConverter, IClientCallInfo callInfo );
    }
}
