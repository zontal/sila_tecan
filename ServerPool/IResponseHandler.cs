using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Tecan.Sila2.ServerPooling
{
    internal interface IResponseHandler
    {
        bool ProcessMessage( SilaServerMessage serverMessage );

        void Cancel();
    }

    internal interface IResponseHandler<T> : IResponseHandler
    {
        Task<T> ResponseTask { get; }
    }
}
