using System;
using System.ComponentModel;

namespace Tecan.Sila2.Generator.Test.TemperatureController
{
    [SilaFeature(true, "Test")]
    [SilaIdentifier("TemperatureController")]
    [SilaDisplayName("Temperature Controller")]
    [SilaDescription("Controlling and retrieving the temperature.")]
    public interface ITemperatureController : INotifyPropertyChanged
    {
        [SilaDisplayName("Control Temperature")]
        [SilaDescription("Control the Temperature gradually to a set target,")]
        [Throws(typeof(TemperatureNotReachableException))]
        [Throws(typeof(ControlInterruptedException))]
        IObservableCommand ControlTemperature([SilaDisplayName("Target Temperature"),
                                               SilaDescription("The target temperature that the device will try to reach. Note that the command might\nbe completed at a temperature that is close enough."),
                                               Unit("K", Kelvin = 1), MinimalExclusive(273.0), MaximalInclusive(373.0)] double targetTemperature);

        [SilaDisplayName("Current Temperature")]
        [SilaDescription("Current Temperature as measured by the controller.")]
        [Dynamic(1000)]
        [Unit("K", Kelvin = 1)]
        [MinimalExclusive(273.0)]
        [MaximalInclusive(373.0)]
        double CurrentTemperature { get; }
    }

    [SilaDisplayName("Temperature Not Reachable")]
    [SilaDescription("The ambient conditions prohibit the device from reaching the target temperature.")]
    public class TemperatureNotReachableException : Exception
    {
        public TemperatureNotReachableException(string message) : base(message) { }
    }

    [SilaDisplayName("Control Interrupted")]
    [SilaDescription("The control of temperature couldn't be finished as it was interrupted by another target temperature.")]
    public class ControlInterruptedException : Exception
    {
        public ControlInterruptedException(string message) : base(message) { }
    }
}
