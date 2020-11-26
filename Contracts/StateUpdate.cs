using System;

namespace Tecan.Sila2
{
    /// <summary>
    /// Describes an update of an observable command
    /// </summary>
    public readonly struct StateUpdate
    {
        /// <summary>
        /// Creates new even arguments
        /// </summary>
        /// <param name="progress">The progress of the observable command</param>
        /// <param name="estimatedRemainingTime">The estimated remaining time</param>
        /// <param name="state">The state of the command</param>
        public StateUpdate(double progress, TimeSpan estimatedRemainingTime, CommandState state)
        {
            Progress = progress;
            EstimatedRemainingTime = estimatedRemainingTime;
            State = state;
        }

        /// <summary>
        /// Gets the current progress of the command in [0,1]
        /// </summary>
        public double Progress { get; }

        /// <summary>
        /// Gets an estimation how long it will take for the command to complete
        /// </summary>
        public TimeSpan EstimatedRemainingTime { get; }

        /// <summary>
        /// Gets the current state of the command
        /// </summary>
        public CommandState State { get; }
    }
}
