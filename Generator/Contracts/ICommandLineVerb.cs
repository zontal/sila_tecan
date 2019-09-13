using System.ComponentModel.Composition.Hosting;

namespace Tecan.Sila2.Generator.Contracts
{
    public interface ICommandLineVerb
    {
        void Execute(CompositionContainer container);
    }
}
