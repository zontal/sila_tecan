using System;
using Tecan.Sila2;

[assembly: VendorUri("http://www.tci.uni-hannover.de")]

namespace SampleContracts
{

    // ---- Feature: SampleService --------------------------

    [SilaFeature(true, "demo")]
    [SilaIdentifier("SampleService")]
    [SilaDisplayName("Sample Service")]
    [SilaDescription("TCI Example SiLA2 Service using the sila_tecan library -- Service to control nothing :)")]

    // ------------------------------------------------------


    public interface ISampleService
    {

        // ---- Command: ToUppercase ---------------------------

        [SilaIdentifier("ToUppercase")]
        [SilaDisplayName("To Uppercase")]
        [SilaDescription("Converts a string to uppercase")]

        [return: SilaIdentifier("Uppercase")]
        [return: SilaDisplayName("Uppercase")]
        [return: SilaDescription("Input converted to Uppercase")]

        string ToUppercase
        (
            [SilaIdentifier("Input")]
            [SilaDisplayName("Input")]
            [SilaDescription("Input string to convert")]
            string Input
        );

        // ------------------------------------------------------


        // ---- Property: Version -------------------------------

        [SilaIdentifier("Version")]
        [SilaDisplayName("Version")]
        [SilaDescription("Version string in format major.minor")]

        string Version { get; }

        // ------------------------------------------------------


        // ---- Dynamic Property: CurrentTime -------------------

        [SilaIdentifier("CurrentTime")]
        [SilaDisplayName("Current Time")]
        [SilaDescription("Get the current time")]
        [Dynamic(500)]

        string CurrentTime { get; }

        // ------------------------------------------------------


        // ---- Observable Command: CountTo ------------------

        [SilaIdentifier("CountTo")]
        [SilaDisplayName("Count To")]
        [SilaDescription("Counts up to a given number")]

        [return: SilaIdentifier("CountToResponses")]
        [return: SilaDisplayName("Count To Responses")]
        [return: SilaDescription("Responses from CountTo command")]
        
        [Throws(typeof(InvalidNumberException))]

        IIntermediateObservableCommand<CountToIntermediates, CountToResponses> CountTo
        (
            [SilaIdentifier("Target")]
            [SilaDisplayName("Target")]
            [SilaDescription("Number to count to")]
            int Target,

            [SilaIdentifier("Message")]
            [SilaDisplayName("Message")]
            [SilaDescription("User defined message")]
            string Message
        );

        // ------------------------------------------------------

    }


    // ---- Observable Command: CountTo (Data Structures) ----

    [SilaIdentifier("CountToIntermediates")]
    [SilaDisplayName("Count To Intermediate Responses")]
    [SilaDescription("Intermediate Responses from CountTo command")]
    public struct CountToIntermediates
    {
        public CountToIntermediates(long CurrentValue, string Message) =>
            (this.CurrentValue, this.Message) = (CurrentValue, Message);

        [SilaIdentifier("CurrentValue")]
        [SilaDisplayName("Current Value")]
        [SilaDescription("The Current Value")]
        public long CurrentValue { get; }

        [SilaIdentifier("Message")]
        [SilaDisplayName("Message")]
        [SilaDescription("Message from the server")]
        public string Message { get; }
    }


    [SilaIdentifier("CountToResponses")]
    [SilaDisplayName("Count To Responses")]
    [SilaDescription("Responses from CountTo command")]
    public struct CountToResponses
    {
        public CountToResponses(long FinalValue, string FinalMessage) =>
            (this.FinalValue, this.FinalMessage) = (FinalValue, FinalMessage);

        [SilaIdentifier("FinalValue")]
        [SilaDisplayName("Final Value")]
        [SilaDescription("The Final Value")]
        public long FinalValue { get; }

        [SilaIdentifier("FinalMessage")]
        [SilaDisplayName("Final Message")]
        [SilaDescription("Final message from the server")]
        public string FinalMessage { get; }
    }


    [SilaDisplayName("InvalidNumberException")]
    [SilaDescription("The specified number was not valid")]
    public class InvalidNumberException : Exception
    {
        public InvalidNumberException(string message) : base(message) { }
    }

    // ------------------------------------------------------
    
}
