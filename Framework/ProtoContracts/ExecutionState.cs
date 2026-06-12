using System;
using ProtoBuf;

namespace Tecan.Sila2
{
    /// <summary>
    /// Data transfer object type for the execution state
    /// </summary>
    [ProtoContract()]
    public class ExecutionState
    {
        /// <summary>
        /// Creates an empty data transfer object
        /// </summary>
        public ExecutionState() { }

        /// <summary>
        /// Creates a data transfer object
        /// </summary>
        /// <param name="status">The status of the command execution</param>
        /// <param name="progress">The curent progress</param>
        /// <param name="estimatedRemainingTime">The estimated remaining time</param>
        public ExecutionState( CommandStatus status, double progress, TimeSpan estimatedRemainingTime )
        {
            Status = status;
            Progress = progress;
            EstimatedRemainingTime = estimatedRemainingTime;
        }

        /// <summary>
        /// Gets or sets the command state
        /// </summary>
        [ProtoMember( 1, Name = "commandStatus" )]
        public CommandStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the progress
        /// </summary>
        [ProtoMember( 2, Name = @"progress" )]
        public RealDto Progress { get; set; }

        /// <summary>
        /// Gets or sets the estimated remaining time
        /// </summary>
        [ProtoMember( 3, Name = "estimatedRemainingTime" )]
        public DurationDto EstimatedRemainingTime { get; set; }

        /// <summary>
        /// Gets or sets the updated lifetime of execution
        /// </summary>
        [ProtoMember( 4, Name = "updatedLifetimeOfExecution" )]
        public DurationDto UpdatedLifetimeOfExecution { get; set; }

        /// <summary>
        /// Denotes the possible states of execution for an observable command
        /// </summary>
        [ProtoContract()]
        public enum CommandStatus
        {
            /// <summary>
            /// The command is waiting for execution
            /// </summary>
            [ProtoEnum( Name = @"waiting" )]
            Waiting = 0,
            /// <summary>
            /// The command is running
            /// </summary>
            [ProtoEnum( Name = @"running" )]
            Running = 1,
            /// <summary>
            /// The command finished successfully
            /// </summary>
            [ProtoEnum( Name = @"finishedSuccessfully" )]
            FinishedSuccessfully = 2,
            /// <summary>
            /// The command finished with error
            /// </summary>
            [ProtoEnum( Name = @"finishedWithError" )]
            FinishedWithError = 3,
        }

        /// <summary>
        /// True, if the execution state indicates that the command finished, otherwise False
        /// </summary>
        public bool IsFinished => Status == CommandStatus.FinishedSuccessfully
            || Status == CommandStatus.FinishedWithError;
    }
}
