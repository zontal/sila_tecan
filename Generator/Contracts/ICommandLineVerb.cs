using System.ComponentModel.Composition.Hosting;

namespace Tecan.Sila2.Generator.Contracts
{
    /// <summary>
    /// Denotes an interface for a command line verb
    /// </summary>
    public interface ICommandLineVerb
    {
        /// <summary>
        /// Executes the command line with the given MEF container
        /// </summary>
        /// <param name="container">The container from which implementations can be drawn</param>
        void Execute(CompositionContainer container);
    }
}
