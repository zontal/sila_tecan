using System;
using System.Threading.Tasks;
using System.ComponentModel.Composition;

using SampleContracts;
using Tecan.Sila2;

namespace SampleImplementation
{

    [Export(typeof(ISampleService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class SampleService : ISampleService
    {

        public string Version
        {
            get =>
                "0.1";
        }



        public string CurrentTime
        {
            get =>
                System.DateTime.Now.ToShortTimeString();
        }



        public string ToUppercase(string Input) =>
            Input.ToUpper();



        public IIntermediateObservableCommand<CountToIntermediates, CountToResponses> CountTo(int Target, string Message) =>
            new CountToCommand(Target, Message);

        private class CountToCommand : IIntermediateObservableCommand<CountToIntermediates, CountToResponses>
        {
            private async Task<CountToResponses> Run(int Target, string Message)
            {
                Console.WriteLine($"Counting up to {Target}");

                if (Target == 42)
                    throw new InvalidNumberException($"Can't count to {Target}! We do not have enough computing power...");

                for (int i = 1; i <= Target; i++)
                {

                    StateUpdateReceived?.Invoke(this, new StateUpdateEventArgs
                    (
                        progress: i / (double)Target,
                        estimatedRemainingTime: TimeSpan.FromSeconds(Target - i),
                        state: CommandState.Running
                    ));

                    IntermediateResponse?.Invoke(this, new IntermediateResponseEventArgs<CountToIntermediates>
                    (
                        new CountToIntermediates
                        (
                            CurrentValue: i,
                            Message: $"Counting to {Target} while repeating {Message}")
                    ));

                    Console.WriteLine(i);
                    await Task.Delay(1000);

                }

                StateUpdateReceived?.Invoke(this, new StateUpdateEventArgs
                (
                    progress: 1,
                    estimatedRemainingTime: TimeSpan.Zero,
                    state: CommandState.FinishedSuccess
                ));


                return new CountToResponses
                (
                    FinalValue: Target,
                    FinalMessage: $"Done counting. Stopped repeating {Message}!"
                );

            }

            public CountToCommand(int Target, string Message) =>
                Response = Run(Target, Message);

            public event EventHandler<StateUpdateEventArgs> StateUpdateReceived;
            public event EventHandler<IntermediateResponseEventArgs<CountToIntermediates>> IntermediateResponse;

            public Task<CountToResponses> Response { get; }
            Task IObservableCommand.Response => (Task)Response;
        }

    }

}

