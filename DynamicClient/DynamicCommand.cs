using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Tecan.Sila2.Cancellation.CancelController;
using Tecan.Sila2.Client;

namespace Tecan.Sila2.DynamicClient
{
    /// <summary>
    /// Denotes a wrapper for a command running on a remote client
    /// </summary>
    public class DynamicCommand : IObservableCommand<DynamicObjectProperty>
    {
        private const string CancelControllerIdentifier = "org.silastandard/core.commands/CancelController/v1";

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="innerCommand">The inner command</param>
        /// <param name="context">The context in which the command is issued</param>
        public DynamicCommand( IObservableCommand<DynamicObjectProperty> innerCommand, FeatureContext context )
        {
            InnerCommand = innerCommand;
            Context = context;
        }

        /// <summary>
        /// The context in which the command is issued
        /// </summary>
        public FeatureContext Context
        {
            get;
        }

        /// <inheritdoc />
        public StateUpdate State => InnerCommand.State;

        /// <inheritdoc />
        public ChannelReader<StateUpdate> StateUpdates => InnerCommand.StateUpdates;

        /// <inheritdoc />
        public void Start()
        {
            InnerCommand.Start();
        }

        /// <summary>
        /// Gets the underlying inner command
        /// </summary>
        protected IObservableCommand<DynamicObjectProperty> InnerCommand { get; }

        /// <inheritdoc />
        public bool IsStarted => InnerCommand.IsStarted;

        /// <summary>
        /// Gets the execution id of the current command
        /// </summary>
        public CommandExecutionUuid CommandId => ((IClientCommand)InnerCommand).CommandId;

        /// <inheritdoc />
        public Task<DynamicObjectProperty> Response => InnerCommand.Response;

        Task IObservableCommand.Response => ((IObservableCommand)InnerCommand).Response;

        /// <inheritdoc />
        public CancellationToken CancellationToken => InnerCommand.CancellationToken;

        /// <inheritdoc />
        public bool IsCancellationSupported => Context.ServerData.Features.Any( f => f.FullyQualifiedIdentifier == CancelControllerIdentifier );

        /// <inheritdoc />
        /// <remarks>This cancels the command also remotely.</remarks>
        public void Cancel()
        {
            if( IsCancellationSupported )
            {
                var client = new CancelControllerClient( Context.ServerData.Channel, Context.ExecutionManager );
                client.CancelCommand( ((IClientCommand)InnerCommand).CommandId );
            }
            InnerCommand.Cancel();
        }
    }

    /// <summary>
    /// Denotes a wrapper for a command running on a remote client that produces intermediate results
    /// </summary>

    public class DynamicIntermediateCommand : DynamicCommand, IIntermediateObservableCommand<DynamicObjectProperty, DynamicObjectProperty>
    {
        /// <inheritdoc />
        public DynamicIntermediateCommand( IIntermediateObservableCommand<DynamicObjectProperty, DynamicObjectProperty> innerCommand, FeatureContext context) : base(innerCommand, context)
        {
        }

        /// <inheritdoc />
        public ChannelReader<DynamicObjectProperty> IntermediateValues => ((IIntermediateObservableCommand<DynamicObjectProperty>)InnerCommand).IntermediateValues;
    }
}
