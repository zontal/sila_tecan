using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Tecan.Sila2.IntegrationTests
{
    internal class ObservableTestCommand : IObservableCommand<int>
    {
        public ObservableTestCommand()
        {
            Response = Run();
        }

        private async Task<int> Run()
        {
            await Task.Delay( TimeSpan.FromSeconds( 1 ) );
            StateUpdateReceived?.Invoke( this, new StateUpdateEventArgs( 0.8, TimeSpan.FromSeconds( 23 ), CommandState.Running ) );
            await Task.Delay( 100 );
            StateUpdateReceived?.Invoke( this, new StateUpdateEventArgs( 0.7, TimeSpan.FromSeconds( 42 ), CommandState.Running ) );
            await Task.Delay( 100 );
            StateUpdateReceived?.Invoke( this, new StateUpdateEventArgs( 1, TimeSpan.Zero, CommandState.FinishedSuccess ) );
            return 42;
        }

        public event EventHandler<StateUpdateEventArgs> StateUpdateReceived;
        public Task<int> Response
        {
            get;
        }

        Task IObservableCommand.Response => Response;
    }
}
