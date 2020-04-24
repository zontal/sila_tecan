using System;
using System.Threading.Tasks;
using System.ComponentModel.Composition;

using SampleContracts;
using Tecan.Sila2;
using Tecan.Sila2.Server;
using System.Threading.Channels;
using System.Threading;

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
            ObservableCommands.Create<CountToIntermediates, CountToResponses>((updateState, sendIntermediateResult) =>
                RunCountToCore(Target, Message, updateState, sendIntermediateResult)
            );

        private async Task<CountToResponses> RunCountToCore(int Target, string Message, Action<double, TimeSpan, CommandState> updateState, Action<CountToIntermediates> sendIntermediateResult)
        {
            Console.WriteLine($"Counting up tto {Target}");

            updateState(0, TimeSpan.FromHours(2), CommandState.NotStarted);

            if (Target == 42)
                throw new InvalidNumberException($"Can't count to {Target}! We do not have enough computing power...");

            for (int i = 1; i <= Target; i++)
            {
                updateState(i / (double)Target, TimeSpan.FromSeconds(Target - i), CommandState.Running);
                sendIntermediateResult(new CountToIntermediates(i, Message));
                Console.WriteLine($"CurrentValue={i}");
                await Task.Delay(1000);
            }

            return new CountToResponses(Target, Message);

        }

    }

}

