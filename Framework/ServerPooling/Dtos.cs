using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;
using Tecan.Sila2.Binary;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.ServerPooling
{
    /// <summary>
    /// Denotes the data transfer object for server messages
    /// </summary>
    [ProtoContract()]
    public partial class SilaServerMessage
    {
        private DiscriminatedUnionObject __pbn__message;

        /// <summary>
        /// Gets or sets the unique identifier of the request
        /// </summary>
        [ProtoMember( 1 )]
        [System.ComponentModel.DefaultValue( "" )]
        public string RequestUuid { get; set; } = "";

        /// <summary>
        /// Gets or sets the message details for a command response
        /// </summary>
        [ProtoMember( 2 )]
        public CommandResponse CommandResponse
        {
            get => __pbn__message.Is( 2 ) ? ((CommandResponse)__pbn__message.Object) : default;
            set => __pbn__message = new DiscriminatedUnionObject( 2, value );
        }

        /// <summary>
        /// Gets or sets the observable command confirmation details
        /// </summary>
        [ProtoMember( 3 )]
        public ObservableCommandConfirmation ObservableCommandConfirmation
        {
            get => __pbn__message.Is( 3 ) ? ((ObservableCommandConfirmation)__pbn__message.Object) : default;
            set => __pbn__message = new DiscriminatedUnionObject( 3, value );
        }

        /// <summary>
        /// Gets or sets the observable command execution info details
        /// </summary>
        [ProtoMember( 4 )]
        public ObservableCommandExecutionInfo ObservableCommandExecutionInfo
        {
            get => __pbn__message.Is( 4 ) ? ((ObservableCommandExecutionInfo)__pbn__message.Object) : default;
            set => __pbn__message = new DiscriminatedUnionObject( 4, value );
        }

        /// <summary>
        /// Gets or sets the message details for an observable command intermediate response
        /// </summary>
        [ProtoMember( 5 )]
        public ObservableCommandResponse ObservableCommandIntermediateResponse
        {
            get => __pbn__message.Is( 5 ) ? ((ObservableCommandResponse)__pbn__message.Object) : default;
            set => __pbn__message = new DiscriminatedUnionObject( 5, value );
        }

        /// <summary>
        /// Gets or sets the observable command response details
        /// </summary>
        [ProtoMember( 6 )]
        public ObservableCommandResponse ObservableCommandResponse
        {
            get => __pbn__message.Is( 6 ) ? ((ObservableCommandResponse)__pbn__message.Object) : default;
            set => __pbn__message = new DiscriminatedUnionObject( 6, value );
        }

        /// <summary>
        /// Gets or sets the metadata response details
        /// </summary>
        [ProtoMember( 7 )]
        public GetFCPAffectedByMetadataResponse MetadataResponse
        {
            get => __pbn__message.Is( 7 ) ? ((GetFCPAffectedByMetadataResponse)__pbn__message.Object) : default;
            set => __pbn__message = new DiscriminatedUnionObject( 7, value );
        }

        /// <summary>
        /// Gets or sets the property value details
        /// </summary>
        [ProtoMember( 8 )]
        public PropertyValue PropertyValue
        {
            get => __pbn__message.Is( 8 ) ? ((PropertyValue)__pbn__message.Object) : default;
            set => __pbn__message = new DiscriminatedUnionObject( 8, value );
        }

        /// <summary>
        /// Gets or sets the observable property value details
        /// </summary>
        [ProtoMember( 9 )]
        public PropertyValue ObservablePropertyValue
        {
            get => __pbn__message.Is( 9 ) ? ((PropertyValue)__pbn__message.Object) : default;
            set => __pbn__message = new DiscriminatedUnionObject( 9, value );
        }

        /// <summary>
        /// Gets or sets the create binary response details
        /// </summary>
        [ProtoMember( 10 )]
        public CreateBinaryResponseDto CreateBinaryResponse
        {
            get => __pbn__message.Is( 10 ) ? ((CreateBinaryResponseDto)__pbn__message.Object) : default;
            set => __pbn__message = new DiscriminatedUnionObject( 10, value );
        }

        /// <summary>
        /// Gets or sets the upload chunk message details
        /// </summary>
        [ProtoMember( 11 )]
        public UploadChunkResponseDto UploadChunkResponse
        {
            get => __pbn__message.Is( 11 ) ? ((UploadChunkResponseDto)__pbn__message.Object) : default;
            set => __pbn__message = new DiscriminatedUnionObject( 11, value );
        }

        /// <summary>
        /// Gets or sets the delete binary response details
        /// </summary>
        [ProtoMember( 12 )]
        public EmptyMessage DeleteBinaryResponse
        {
            get => __pbn__message.Is( 12 ) ? ((EmptyMessage)__pbn__message.Object) : default;
            set => __pbn__message = new DiscriminatedUnionObject( 12, value );
        }

        /// <summary>
        /// Gets or sets the get binary response details
        /// </summary>
        [ProtoMember( 13 )]
        public GetBinaryInfoResponseDto GetBinaryResponse
        {
            get => __pbn__message.Is( 13 ) ? ((GetBinaryInfoResponseDto)__pbn__message.Object) : default;
            set => __pbn__message = new DiscriminatedUnionObject( 13, value );
        }

        /// <summary>
        /// Gets the get chunk response details
        /// </summary>
        [ProtoMember( 14 )]
        public GetChunkResponseDto GetChunkResponse
        {
            get => __pbn__message.Is( 14 ) ? ((GetChunkResponseDto)__pbn__message.Object) : default;
            set => __pbn__message = new DiscriminatedUnionObject( 14, value );
        }

        /// <summary>
        /// Gets the binary error information
        /// </summary>
        [ProtoMember( 15 )]
        public BinaryTransferError BinaryError
        {
            get => __pbn__message.Is( 15 ) ? ((BinaryTransferError)__pbn__message.Object) : default;
            set => __pbn__message = new DiscriminatedUnionObject( 15, value );
        }

        /// <summary>
        /// Gets or sets the command error details
        /// </summary>
        [ProtoMember( 16 )]
        public SiLAErrorDto CommandError
        {
            get => __pbn__message.Is( 16 ) ? ((SiLAErrorDto)__pbn__message.Object) : default;
            set => __pbn__message = new DiscriminatedUnionObject( 16, value );
        }

        /// <summary>
        /// Gets or sets the property error details
        /// </summary>
        [ProtoMember( 17 )]
        public SiLAErrorDto PropertyError
        {
            get => __pbn__message.Is( 17 ) ? ((SiLAErrorDto)__pbn__message.Object) : default;
            set => __pbn__message = new DiscriminatedUnionObject( 17, value );
        }

        /// <summary>
        /// Gets the type of the message
        /// </summary>
        public SilaServerMessageType Type => (SilaServerMessageType)__pbn__message.Discriminator;

    }

    /// <summary>
    /// Denotes the types of messages from a server
    /// </summary>
    public enum SilaServerMessageType
    {
        /// <summary>
        /// No message
        /// </summary>
        None = 0,
        /// <summary>
        /// Server reports command execution successful
        /// </summary>
        CommandResponse = 2,
        /// <summary>
        /// Server confirms execution of observable command
        /// </summary>
        ObservableCommandConfirmation = 3,
        /// <summary>
        /// Server reports state of observable command execution
        /// </summary>
        ObservableCommandExecutionInfo = 4,
        /// <summary>
        /// Server reports intermediate response of observable command
        /// </summary>
        ObservableCommandIntermediateResponse = 5,
        /// <summary>
        /// Server reports result of observable command
        /// </summary>
        ObservableCommandResponse = 6,
        /// <summary>
        /// Server reports metadata usage
        /// </summary>
        MetadataResponse = 7,
        /// <summary>
        /// Server reports property value
        /// </summary>
        PropertyValue = 8,
        /// <summary>
        /// Server reports observable property value
        /// </summary>
        ObservablePropertyValue = 9,
        /// <summary>
        /// Server reports binary created
        /// </summary>
        CreateBinaryResponse = 10,
        /// <summary>
        /// Server reports chunk uploaded
        /// </summary>
        UploadChunkResponse = 11,
        /// <summary>
        /// Server reports binary deleted
        /// </summary>
        DeleteBinaryResponse = 12,
        /// <summary>
        /// Server reports binary details
        /// </summary>
        GetBinaryResponse = 13,
        /// <summary>
        /// Server responds chunk of binary
        /// </summary>
        GetChunkResponse = 14,
        /// <summary>
        /// Messages that there was an error transferring binaries
        /// </summary>
        BinaryError = 15,
        /// <summary>
        /// Server reports error of command
        /// </summary>
        CommandError = 16,
        /// <summary>
        /// Server reports error of property
        /// </summary>
        PropertyError = 17
    }

    /// <summary>
    /// Denotes a message from or to a server pool
    /// </summary>
    [ProtoContract()]
    public partial class SilaClientMessage
    {
        private DiscriminatedUnionObject __pbn__message;

        /// <summary>
        /// Gets the unique identifier of the client message
        /// </summary>
        [ProtoMember( 1 )]
        [System.ComponentModel.DefaultValue( "" )]
        public string RequestUuid { get; set; } = "";

        /// <summary>
        /// Gets or sets the details of a command execution
        /// </summary>
        [ProtoMember( 2 )]
        public CommandExecution CommandExecution
        {
            get => __pbn__message.Is( 2 ) ? ((CommandExecution)__pbn__message.Object) : default;
            set => __pbn__message = new DiscriminatedUnionObject( 2, value );
        }

        /// <summary>
        /// Gets or sets the details of a command initiation
        /// </summary>
        [ProtoMember( 3 )]
        public CommandInitiation CommandInitiation
        {
            get => __pbn__message.Is( 3 ) ? ((CommandInitiation)__pbn__message.Object) : default;
            set => __pbn__message = new DiscriminatedUnionObject( 3, value );
        }

        /// <summary>
        /// Gets or sets the details of a command execution subscription
        /// </summary>
        [ProtoMember( 4 )]
        public CommandExecutionReference CommandExecutionInfoSubscription
        {
            get => __pbn__message.Is( 4 ) ? ((CommandExecutionReference)__pbn__message.Object) : default;
            set => __pbn__message = new DiscriminatedUnionObject( 4, value );
        }

        /// <summary>
        /// Gets or sets the details of a subscription to intermediate responses
        /// </summary>
        [ProtoMember( 5 )]
        public CommandExecutionReference CommandIntermediateResponseSubscription
        {
            get => __pbn__message.Is( 5 ) ? ((CommandExecutionReference)__pbn__message.Object) : default;
            set => __pbn__message = new DiscriminatedUnionObject( 5, value );
        }

        /// <summary>
        /// Gets or sets the details of a request to get the response of a command
        /// </summary>
        [ProtoMember( 6 )]
        public CommandExecutionReference CommandGetResponse
        {
            get => __pbn__message.Is( 6 ) ? ((CommandExecutionReference)__pbn__message.Object) : default;
            set => __pbn__message = new DiscriminatedUnionObject( 6, value );
        }

        /// <summary>
        /// Gets or sets the details of a request for metadata
        /// </summary>
        [ProtoMember( 7 )]
        public GetFCPAffectedByMetadataRequest MetadataRequest
        {
            get => __pbn__message.Is( 7 ) ? ((GetFCPAffectedByMetadataRequest)__pbn__message.Object) : default;
            set => __pbn__message = new DiscriminatedUnionObject( 7, value );
        }

        /// <summary>
        /// Gets or sets the details of a request to read a property
        /// </summary>
        [ProtoMember( 8 )]
        public PropertyRead PropertyRead
        {
            get => __pbn__message.Is( 8 ) ? ((PropertyRead)__pbn__message.Object) : default;
            set => __pbn__message = new DiscriminatedUnionObject( 8, value );
        }

        /// <summary>
        /// Gets or sets the details of a request to subscribe to a property
        /// </summary>
        [ProtoMember( 9 )]
        public PropertyRead PropertySubscription
        {
            get => __pbn__message.Is( 9 ) ? ((PropertyRead)__pbn__message.Object) : default;
            set => __pbn__message = new DiscriminatedUnionObject( 9, value );
        }

        /// <summary>
        /// Gets or sets the details of a request to cancel the subscription for command execution state updates
        /// </summary>
        [ProtoMember( 10 )]
        public EmptyMessage CancelCommandExecutionInfoSubscription
        {
            get => __pbn__message.Is( 10 ) ? ((EmptyMessage)__pbn__message.Object) : default;
            set => __pbn__message = new DiscriminatedUnionObject( 10, value );
        }

        /// <summary>
        /// Gets or sets the details of a request to cancel the subscription of command intermediate responses
        /// </summary>
        [ProtoMember( 11 )]
        public EmptyMessage CancelCommandIntermediateResponseSubscription
        {
            get => __pbn__message.Is( 11 ) ? ((EmptyMessage)__pbn__message.Object) : default;
            set => __pbn__message = new DiscriminatedUnionObject( 11, value );
        }

        /// <summary>
        /// Gets or sets the details of a request to cancel a property subscription
        /// </summary>
        [ProtoMember( 12 )]
        public EmptyMessage CancelPropertySubscription
        {
            get => __pbn__message.Is( 12 ) ? ((EmptyMessage)__pbn__message.Object) : default;
            set => __pbn__message = new DiscriminatedUnionObject( 12, value );
        }

        /// <summary>
        /// Gets or sets the details of a request to create a binary
        /// </summary>
        [ProtoMember( 13 )]
        public CreateBinaryUploadRequest CreateBinaryRequest
        {
            get => __pbn__message.Is( 13 ) ? ((CreateBinaryUploadRequest)__pbn__message.Object) : default;
            set => __pbn__message = new DiscriminatedUnionObject( 13, value );
        }

        /// <summary>
        /// Gets or sets the request to delete a binary
        /// </summary>
        [ProtoMember( 14 )]
        public DeleteBinaryRequestDto DeleteUploadedBinaryRequest
        {
            get => __pbn__message.Is( 14 ) ? ((DeleteBinaryRequestDto)__pbn__message.Object) : default;
            set => __pbn__message = new DiscriminatedUnionObject( 14, value );
        }

        /// <summary>
        /// Gets or sets the details of a request to upload a chunk
        /// </summary>
        [ProtoMember( 15 )]
        public UploadChunkRequestDto UploadChunkRequest
        {
            get => __pbn__message.Is( 15 ) ? ((UploadChunkRequestDto)__pbn__message.Object) : default;
            set => __pbn__message = new DiscriminatedUnionObject( 15, value );
        }

        /// <summary>
        /// Gets or sets the request for information of a binary
        /// </summary>
        [ProtoMember( 16 )]
        public GetBinaryInfoRequestDto GetBinaryInfoRequest
        {
            get => __pbn__message.Is( 16 ) ? ((GetBinaryInfoRequestDto)__pbn__message.Object) : default;
            set => __pbn__message = new DiscriminatedUnionObject( 16, value );
        }

        /// <summary>
        /// Gets or sets the request to get a chunk of a binary
        /// </summary>
        [ProtoMember( 17 )]
        public GetChunkRequestDto GetChunkRequest
        {
            get => __pbn__message.Is( 17 ) ? ((GetChunkRequestDto)__pbn__message.Object) : default;
            set => __pbn__message = new DiscriminatedUnionObject( 17, value );
        }

        /// <summary>
        /// Gets or sets the request to delete a binary
        /// </summary>
        [ProtoMember( 18 )]
        public DeleteBinaryRequestDto DeleteDownloadedBinaryRequest
        {
            get => __pbn__message.Is( 18 ) ? ((DeleteBinaryRequestDto)__pbn__message.Object) : default;
            set => __pbn__message = new DiscriminatedUnionObject( 14, value );
        }

        /// <summary>
        /// Indicates the type of client message
        /// </summary>
        public SilaClientMessageType Type => (SilaClientMessageType)__pbn__message.Discriminator;

    }

    /// <summary>
    /// Denotes the different types of client messages
    /// </summary>
    public enum SilaClientMessageType
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,
        /// <summary>
        /// Request to execute an unobservable command
        /// </summary>
        CommandExecution = 2,
        /// <summary>
        /// Requests to initiate an observable command
        /// </summary>
        CommandInitiation = 3,
        /// <summary>
        /// Requests to subscribe to execution state updates
        /// </summary>
        CommandExecutionInfoSubscription = 4,
        /// <summary>
        /// Requests to subscribe to intermediate responses
        /// </summary>
        CommandIntermediateResponseSubscription = 5,
        /// <summary>
        /// Requests to get the response of an observable command
        /// </summary>
        CommandGetResponse = 6,
        /// <summary>
        /// Requests usage of metadata
        /// </summary>
        MetadataRequest = 7,
        /// <summary>
        /// Requests to read a property
        /// </summary>
        PropertyRead = 8,
        /// <summary>
        /// Requests to subscribe to a property
        /// </summary>
        PropertySubscription = 9,
        /// <summary>
        /// Requests to cancel the subscription for an execution info
        /// </summary>
        CancelCommandExecutionInfoSubscription = 10,
        /// <summary>
        /// Requests to cancel the subscription for intermediate responses
        /// </summary>
        CancelCommandIntermediateResponseSubscription = 11,
        /// <summary>
        /// Requests to cancel the subscription to a property
        /// </summary>
        CancelPropertySubscription = 12,
        /// <summary>
        /// Requests to create a binary
        /// </summary>
        CreateBinaryRequest = 13,
        /// <summary>
        /// Requests to delete an uploaded binary
        /// </summary>
        DeleteUploadedBinaryRequest = 14,
        /// <summary>
        /// Requests to upload a binary chunk
        /// </summary>
        UploadChunkRequest = 15,
        /// <summary>
        /// Requests to get binary information
        /// </summary>
        GetBinaryInfoRequest = 16,
        /// <summary>
        /// Requests to obtain a chunk of a binary file
        /// </summary>
        GetChunkRequest = 17,
        /// <summary>
        /// Requests to delete a binary after download
        /// </summary>
        DeleteDownloadedBinaryRequest = 18,
    }

    /// <summary>
    /// Denotes the data transfer object to request a command execution
    /// </summary>
    [ProtoContract()]
    public partial class CommandExecution
    {
        /// <summary>
        /// Gets or sets the fully qualified command identifier
        /// </summary>
        [ProtoMember( 1 )]
        [System.ComponentModel.DefaultValue( "" )]
        public string FullyQualifiedCommandId { get; set; } = "";

        /// <summary>
        /// Gets or sets the command parameters
        /// </summary>
        [ProtoMember( 2 )]
        public CommandParameter CommandParameter { get; set; }

    }

    /// <summary>
    /// Denotes the data transfer object to transmit the command parameters
    /// </summary>
    [ProtoContract()]
    public partial class CommandParameter : IMetadataRepository
    {
        /// <summary>
        /// Gets or sets the metadata for the command execution
        /// </summary>
        [ProtoMember( 1, Name = @"metadata" )]
        public System.Collections.Generic.List<Metadata> Metadata { get; } = new System.Collections.Generic.List<Metadata>();

        /// <summary>
        /// Gets or sets the command parameters as binary
        /// </summary>
        [ProtoMember( 2, Name = @"parameters" )]
        public byte[] Parameters { get; set; }

        /// <inheritdoc />
        public int Count => Metadata.Count;

        /// <inheritdoc />
        public bool TryGetMetadata( string metadataIdentifier, out byte[] metadata )
        {
            foreach(var entry in Metadata)
            {
                if (entry.FullyQualifiedMetadataId == metadataIdentifier)
                {
                    metadata = entry.Value;
                    return true;
                }
            }
            metadata = null;
            return false;
        }
    }

    /// <summary>
    /// Denotes a data transfer object for metadata
    /// </summary>
    [ProtoContract()]
    public partial class Metadata
    {
        /// <summary>
        /// Gets or sets the fully qualified identifier of the metadata
        /// </summary>
        [ProtoMember( 1 )]
        [System.ComponentModel.DefaultValue( "" )]
        public string FullyQualifiedMetadataId { get; set; } = "";

        /// <summary>
        /// Gets or sets the value of the metadata as bytes
        /// </summary>
        [ProtoMember( 2, Name = @"value" )]
        public byte[] Value { get; set; }

    }

    /// <summary>
    /// Denotes the data transfer object to initiate an observable command
    /// </summary>
    [ProtoContract()]
    public partial class CommandInitiation
    {
        /// <summary>
        /// Gets or sets the fully qualified command identifier
        /// </summary>
        [ProtoMember( 1 )]
        [System.ComponentModel.DefaultValue( "" )]
        public string FullyQualifiedCommandId { get; set; } = "";

        /// <summary>
        /// Gets or sets the command parameters
        /// </summary>
        [ProtoMember( 2 )]
        public CommandParameter CommandParameter { get; set; }

    }

    /// <summary>
    /// Denotes the data transfer object for reference of an observable command
    /// </summary>
    [ProtoContract()]
    public partial class CommandExecutionReference
    {
        /// <summary>
        /// Gets or sets the unique identifier of the observable command execution
        /// </summary>
        [ProtoMember( 1 )]
        public CommandExecutionUuid ExecutionUUID { get; set; }

    }

    /// <summary>
    /// Denotes the data transfer object for a command response
    /// </summary>
    [ProtoContract()]
    public partial class CommandResponse
    {
        /// <summary>
        /// Gets or sets the command response
        /// </summary>
        [ProtoMember( 1, Name = @"result" )]
        public byte[] Result { get; set; }

    }

    /// <summary>
    /// Denotes the confirmation of an observable command
    /// </summary>
    [ProtoContract()]
    public partial class ObservableCommandConfirmation
    {
        /// <summary>
        /// Gets or sets the actual command confirmation
        /// </summary>
        [ProtoMember( 1 )]
        public CommandConfirmation CommandConfirmation { get; set; }

    }

    /// <summary>
    /// Denotes the response of an execution info request
    /// </summary>
    [ProtoContract()]
    public partial class ObservableCommandExecutionInfo
    {
        /// <summary>
        /// Gets or sets the execution identifier
        /// </summary>
        [ProtoMember( 1 )]
        public CommandExecutionUuid ExecutionUUID { get; set; }

        /// <summary>
        /// Gets or sets the execution info
        /// </summary>
        [ProtoMember( 2 )]
        public ExecutionState ExecutionInfo { get; set; }

    }

    /// <summary>
    /// Denotes the response with the result of an observable command
    /// </summary>
    [ProtoContract()]
    public partial class ObservableCommandResponse
    {
        /// <summary>
        /// Gets or sets the execution identifier
        /// </summary>
        [ProtoMember( 1 )]
        public CommandExecutionUuid ExecutionUUID { get; set; }

        /// <summary>
        /// Gets or sets the actual command result
        /// </summary>
        [ProtoMember( 2, Name = @"result" )]
        public byte[] Result { get; set; }

    }

    /// <summary>
    /// Denotes the request for a list of affected metadata
    /// </summary>
    [ProtoContract()]
    public partial class GetFCPAffectedByMetadataRequest
    {
        /// <summary>
        /// Gets or sets the identifier of the metadata that is requested
        /// </summary>
        [ProtoMember( 1 )]
        [System.ComponentModel.DefaultValue( "" )]
        public string FullyQualifiedMetadataId { get; set; } = "";

    }

    /// <summary>
    /// Denotes a response of a metadata query
    /// </summary>
    [ProtoContract()]
    public partial class GetFCPAffectedByMetadataResponse
    {
        /// <summary>
        /// Gets or sets the calls affected by the requested metadata
        /// </summary>
        [ProtoMember( 1 )]
        public List<string> AffectedCalls { get; set; } = new List<string>();

    }

    /// <summary>
    /// Denotes the request to a property
    /// </summary>
    [ProtoContract()]
    public partial class PropertyRead : IMetadataRepository
    {
        /// <summary>
        /// Gets or sets the fully qualified identifier of the property
        /// </summary>
        [ProtoMember( 1 )]
        [System.ComponentModel.DefaultValue( "" )]
        public string FullyQualifiedPropertyId { get; set; } = "";

        /// <summary>
        /// Gets or sets the metadata attached to the property read request
        /// </summary>
        [ProtoMember( 2, Name = @"metadata" )]
        public List<Metadata> Metadatas { get; } = new List<Metadata>();

        /// <inheritdoc />
        public int Count => Metadatas.Count;

        /// <inheritdoc />
        public bool TryGetMetadata( string metadataIdentifier, out byte[] metadata )
        {
            foreach(var entry in Metadatas)
            {
                if(entry.FullyQualifiedMetadataId == metadataIdentifier)
                {
                    metadata = entry.Value;
                    return true;
                }
            }
            metadata = null;
            return false;
        }

    }

    /// <summary>
    /// Denotes the transfer information for a request to create binary data
    /// </summary>
    [ProtoContract]
    public partial class CreateBinaryUploadRequest : IMetadataRepository
    {
        /// <summary>
        /// Gets the list of metadata entries for the create binary request
        /// </summary>
        [ProtoMember( 1 )]
        public List<Metadata> Metadata
        {
            get;
        } = new List<Metadata>();

        /// <summary>
        /// Gets or sets the actual create binary request
        /// </summary>
        [ProtoMember( 2 )]
        public CreateBinaryRequestDto CreateBinaryRequest
        {
            get;
            set;
        }

        /// <inheritdoc />
        public int Count => Metadata.Count;

        /// <inheritdoc />
        public bool TryGetMetadata( string metadataIdentifier, out byte[] metadata )
        {
            foreach(var item in Metadata)
            {
                if (item.FullyQualifiedMetadataId == metadataIdentifier)
                {
                    metadata = item.Value;
                    return true;
                }
            }
            metadata = null;
            return false;
        }
    }

    /// <summary>
    /// Denotes a response with a property value
    /// </summary>
    [ProtoContract()]
    public partial class PropertyValue
    {
        /// <summary>
        /// Gets or sets the actual property result
        /// </summary>
        [ProtoMember( 1, Name = @"result" )]
        public byte[] Result { get; set; }

    }
}
