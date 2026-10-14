using System.Collections.Generic;
using Grpc.Core;

namespace Tecan.Sila2
{
    /// <summary>
    /// gRPC marshallers for the data transfer objects of the SiLA2 framework, based on the <see cref="FrameworkTypeModel"/>
    /// </summary>
    /// <remarks>These marshallers are compatible with trimming and Native AOT.</remarks>
    public static class FrameworkMarshallers
    {
        /// <summary>
        /// The marshaller for command confirmations
        /// </summary>
        public static readonly Marshaller<CommandConfirmation> CommandConfirmation = ProtobufMarshaller.Create<CommandConfirmation>( FrameworkTypeModel.Instance );

        /// <summary>
        /// The marshaller for command execution UUIDs
        /// </summary>
        public static readonly Marshaller<CommandExecutionUuid> CommandExecutionUuid = ProtobufMarshaller.Create<CommandExecutionUuid>( FrameworkTypeModel.Instance );

        /// <summary>
        /// The marshaller for execution states
        /// </summary>
        public static readonly Marshaller<ExecutionState> ExecutionState = ProtobufMarshaller.Create<ExecutionState>( FrameworkTypeModel.Instance );

        /// <summary>
        /// The marshaller for the calls affected by a metadata
        /// </summary>
        /// <remarks>
        /// The affected calls are transmitted as a message with a repeated string field 1, which has the same wire format
        /// as a <see cref="PropertyResponse{T}"/> of a list of strings.
        /// </remarks>
        public static readonly Marshaller<List<StringDto>> AffectedCalls = new Marshaller<List<StringDto>>(
            calls => ByteSerializer.ToByteArray( new PropertyResponse<List<StringDto>>( calls ), FrameworkTypeModel.Instance ),
            data => ByteSerializer.FromByteArray<PropertyResponse<List<StringDto>>>( data, FrameworkTypeModel.Instance ).Value ?? new List<StringDto>() );
    }
}