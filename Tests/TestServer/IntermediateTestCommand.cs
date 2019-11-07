using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Tecan.Sila2.IntegrationTests
{
    internal class IntermediateTestCommand : IIntermediateObservableCommand<int>
    {
        public IntermediateTestCommand()
        {
            Response = Run();
        }

        private async Task Run()
        {
            await Task.Delay( TimeSpan.FromSeconds( 1 ) );
            IntermediateResponse?.Invoke(this, new IntermediateResponseEventArgs<int>(0));
            await Task.Delay( 100 );
            IntermediateResponse?.Invoke( this, new IntermediateResponseEventArgs<int>( 8 ) );
            await Task.Delay( 100 );
            IntermediateResponse?.Invoke( this, new IntermediateResponseEventArgs<int>( 15 ) );
            await Task.Delay( 100 );
            StateUpdateReceived?.Invoke( this, new StateUpdateEventArgs( 1, TimeSpan.Zero, CommandState.FinishedSuccess ) );
        }

        public event EventHandler<StateUpdateEventArgs> StateUpdateReceived;
        public Task Response
        {
            get;
        }

        public event EventHandler<IntermediateResponseEventArgs<int>> IntermediateResponse;
    }
}
