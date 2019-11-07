using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using ProtoBuf;
using Tecan.Sila2.Binary;

namespace Tecan.Sila2
{
    /// <summary>
    /// The transfer object for a string
    /// </summary>
    [ProtoContract()]
    public class StringDto : ISilaTransferObject<string>
    {
        /// <summary>
        /// Creates a new dto
        /// </summary>
        public StringDto() { }

        /// <summary>
        /// Creates a new dto with the given string content
        /// </summary>
        /// <param name="value">The inner value</param>
        /// <param name="store">A binary store to resolve binary data</param>
        public StringDto(string value, IBinaryStore store = null)
        {
            Value = value;
        }

        /// <summary>
        /// The actual value
        /// </summary>
        [ProtoMember(1, Name = @"value")]
        [System.ComponentModel.DefaultValue("")]
        public string Value { get; set; } = "";

        /// <summary>
        /// Packages a string into a dto
        /// </summary>
        /// <param name="value">The string value</param>
        public static implicit operator StringDto(string value)
        {
            return new StringDto(value);
        }

        /// <summary>
        /// Extracts the underlying string
        /// </summary>
        /// <returns>The actual string value</returns>
        public string Extract(IBinaryStore resolver)
        {
            return Value;
        }

        /// <summary>
        /// Gets the validation errors for this dto
        /// </summary>
        /// <returns>null, because the type does not perform validation</returns>
        public string GetValidationErrors()
        {
            return null;
        }

        /// <summary>
        /// Creates a dto for the given string
        /// </summary>
        /// <param name="value">The string value</param>
        /// <param name="store">A binary store</param>
        /// <returns>The encapsulated value</returns>
        public static StringDto Create(string value, IBinaryStore store = null)
        {
            return value;
        }
    }

    /// <summary>
    /// The transfer object for an integer
    /// </summary>
    [ProtoContract()]
    public class IntegerDto : ISilaTransferObject<long>
    {
        /// <summary>
        /// Creates a new integer dto
        /// </summary>
        public IntegerDto() { }

        /// <summary>
        /// Creates a transfer object for the given inner value
        /// </summary>
        /// <param name="value">The inner value</param>
        /// <param name="store">A binary store to resolve binary data</param>
        public IntegerDto(long value, IBinaryStore store = null)
        {
            Value = value;
        }

        /// <summary>
        /// The actual value for this dto
        /// </summary>
        [ProtoMember(1, Name = @"value")]
        public long Value { get; set; }

        /// <summary>
        /// Converts the given integer value to a dto
        /// </summary>
        /// <param name="value">The actual value</param>
        public static implicit operator IntegerDto(long value)
        {
            return new IntegerDto(value);
        }

        /// <summary>
        /// Extracts the value from the dto
        /// </summary>
        /// <param name="resolver">A resolver component to resolve binary data</param>
        /// <returns>The actual value</returns>
        public long Extract(IBinaryStore resolver)
        {
            return Value;
        }

        /// <summary>
        /// Validates the contents of the current dto
        /// </summary>
        /// <returns>Null as no errors have been found</returns>
        public string GetValidationErrors()
        {
            return null;
        }

        /// <summary>
        /// Creates a new dto for the given value
        /// </summary>
        /// <param name="value">The actual value</param>
        /// <param name="store">A binary store</param>
        /// <returns>The encapsulated value</returns>
        public static IntegerDto Create(long value, IBinaryStore store = null)
        {
            return value;
        }
    }

    /// <summary>
    /// The data transfer type for floating numbers
    /// </summary>
    [ProtoContract()]
    public class RealDto : ISilaTransferObject<double>
    {
        /// <summary>
        /// Creates an empty instance
        /// </summary>
        public RealDto() { }

        /// <summary>
        /// Creates a transfer object for the given inner value
        /// </summary>
        /// <param name="value">The inner value</param>
        /// <param name="store">A binary store to resolve binary data</param>
        public RealDto(double value, IBinaryStore store = null)
        {
            Value = value;
        }

        /// <summary>
        /// The actual value for this dto
        /// </summary>
        [ProtoMember(1, Name = @"value")]
        public double Value { get; set; }

        /// <summary>
        /// Encapsulates the given floating point into a dto
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator RealDto(double value)
        {
            return new RealDto(value);
        }

        /// <summary>
        /// Extracts the value from the dto
        /// </summary>
        /// <param name="resolver">A resolver component to resolve binary data</param>
        /// <returns>The actual value</returns>
        public double Extract(IBinaryStore resolver)
        {
            return Value;
        }

        /// <summary>
        /// Validates the contents of the current dto
        /// </summary>
        /// <returns>Null as no errors have been found</returns>
        public string GetValidationErrors()
        {
            return null;
        }

        /// <summary>
        /// Encapsulates the given floating point into a dto
        /// </summary>
        /// <param name="value">The inner value</param>
        /// <param name="store">A binary store</param>
        /// <returns>The encapsulated value</returns>
        public static RealDto Create(double value, IBinaryStore store = null)
        {
            return value;
        }
    }

    /// <summary>
    /// The data transfer type for a boolean value
    /// </summary>
    [ProtoContract()]
    public class BooleanDto : ISilaTransferObject<bool>
    {
        /// <summary>
        /// Creates an empty instance
        /// </summary>
        public BooleanDto() { }

        /// <summary>
        /// Creates a transfer object for the given inner value
        /// </summary>
        /// <param name="value">The inner value</param>
        /// <param name="store">A binary store to resolve binary data</param>
        public BooleanDto(bool value, IBinaryStore store = null)
        {
            Value = value;
        }

        /// <summary>
        /// The actual value for this dto
        /// </summary>
        [ProtoMember(1, Name = @"value")]
        public bool Value { get; set; }

        /// <summary>
        /// Encapsulates the given value into a dto
        /// </summary>
        /// <param name="value">The actual value</param>
        public static implicit operator BooleanDto(bool value)
        {
            return new BooleanDto(value);
        }

        /// <summary>
        /// Extracts the value from the dto
        /// </summary>
        /// <param name="resolver">A resolver component to resolve binary data</param>
        /// <returns>The actual value</returns>
        public bool Extract(IBinaryStore resolver)
        {
            return Value;
        }

        /// <summary>
        /// Validates the contents of the current dto
        /// </summary>
        /// <returns>Null as no errors have been found</returns>
        public string GetValidationErrors()
        {
            return null;
        }

        /// <summary>
        /// Creates a new dto for the given value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="store">A binary store</param>
        /// <returns>The encapsulated value</returns>
        public static BooleanDto Create(bool value, IBinaryStore store = null)
        {
            return value;
        }
    }

    /// <summary>
    /// The data transfer type for binaries
    /// </summary>
    [ProtoContract()]
    public class BinaryDto : ISilaTransferObject<Stream>
    {
        /// <summary>
        /// Creates an empty instance
        /// </summary>
        public BinaryDto() { }

        /// <summary>
        /// Creates a transfer object for the given inner value
        /// </summary>
        /// <param name="data">The inner value</param>
        /// <param name="store">A binary store to resolve binary data</param>
        public BinaryDto(Stream data, IBinaryStore store)
        {
            if (data == null)
            {
                Value = new byte[0];
            }
            else if (store.ShouldStoreBinary(data.Length))
            {
                BinaryTransferUUID = store.StoreBinary(data);
            }
            else
            {
                using (var ms = new MemoryStream())
                {
                    data.CopyTo(ms);
                    Value = ms.ToArray();
                }
            }
        }

        /// <summary>
        /// Creates a transfer object for the given binary array
        /// </summary>
        /// <param name="data">The binary array</param>
        /// <param name="store">A binary store to resolve binary data</param>
        public BinaryDto(byte[] data, IBinaryStore store)
        {
            if (store.ShouldStoreBinary(data.LongLength))
            {
                using (var ms = new MemoryStream(data))
                {
                    BinaryTransferUUID = store.StoreBinary(ms);
                }
            }
            else
            {
                Value = data;
            }
        }

        /// <summary>
        /// Create a binary transfer object for the given file
        /// </summary>
        /// <param name="file">The file that should be stored</param>
        /// <param name="store">A binary store to resolve binary data</param>
        public BinaryDto(FileInfo file, IBinaryStore store)
        {
            BinaryTransferUUID = store.StoreBinary(file);
        }

        /// <summary>
        /// The actual value for this dto
        /// </summary>
        [ProtoMember(1, Name = @"value")]
        public byte[] Value
        {
            get { return __pbn__union.Is(1) ? ((byte[])__pbn__union.Object) : default; }
            set { __pbn__union = new DiscriminatedUnionObject(1, value); }
        }
        /// <summary>
        /// Gets a value indicating whether the value property should be serialized
        /// </summary>
        /// <returns>True, if the value property should be serialized, otherwise False</returns>
        public bool ShouldSerializeValue() => __pbn__union.Is(1);
        /// <summary>
        /// Resets the Value property
        /// </summary>
        public void ResetValue() => DiscriminatedUnionObject.Reset(ref __pbn__union, 1);

        private DiscriminatedUnionObject __pbn__union;

        /// <summary>
        /// Gets the binary storage identifier
        /// </summary>
        [ProtoMember(2)]
        [DefaultValue("")]
        public string BinaryTransferUUID
        {
            get { return __pbn__union.Is(2) ? ((string)__pbn__union.Object) : ""; }
            set { __pbn__union = new DiscriminatedUnionObject(2, value); }
        }
        /// <summary>
        /// Gets a value indicating whether the binary storage identifier property should be serialized
        /// </summary>
        /// <returns>True, if the binary storage identifier property should be serialized, otherwise False</returns>
        public bool ShouldSerializeBinaryTransferUUID() => __pbn__union.Is(2);

        /// <summary>
        /// Resets the binary storage identifier property
        /// </summary>
        public void ResetBinaryTransferUUID() => DiscriminatedUnionObject.Reset(ref __pbn__union, 2);

        /// <summary>
        /// Extracts the value from the dto
        /// </summary>
        /// <param name="resolver">A resolver component to resolve binary data</param>
        /// <returns>The actual value</returns>
        public Stream Extract(IBinaryStore resolver)
        {
            if (ShouldSerializeValue())
            {
                return new MemoryStream(Value);
            }
            else
            {
                return resolver.ResolveStoredBinary(BinaryTransferUUID);
            }
        }

        /// <summary>
        /// Extracts the value from the dto into a byte array
        /// </summary>
        /// <param name="resolver">A resolver component to resolve binary data</param>
        /// <returns>The actual value</returns>
        public byte[] ExtractToBytes(IBinaryStore resolver)
        {
            if( ShouldSerializeValue() )
            {
                return Value;
            }
            else
            {
                using( var ms = new MemoryStream() )
                {
                    resolver.ResolveStoredBinary(BinaryTransferUUID).CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }

        /// <summary>
        /// Extracts the value from the dto into a file info
        /// </summary>
        /// <param name="resolver">A resolver component to resolve binary data</param>
        /// <returns>The actual value</returns>
        public FileInfo ExtractToFileInfo( IBinaryStore resolver )
        {
            if( ShouldSerializeValue() )
            {
                var path = Path.GetTempFileName();
                File.WriteAllBytes(path, Value);
                return new FileInfo(path);
            }
            else
            {
                return resolver.ResolveStoredBinaryFile( BinaryTransferUUID );
            }
        }

        /// <summary>
        /// Validates the contents of the current dto
        /// </summary>
        /// <returns>Null as no errors have been found</returns>
        public string GetValidationErrors()
        {
            return null;
        }
    }

    /// <summary>
    /// The data transfer object for a date
    /// </summary>
    [ProtoContract()]
    public class DateDto : ISilaTransferObject<DateTimeOffset>
    {
        /// <summary>
        /// Creates an empty instance
        /// </summary>
        public DateDto() { }

        /// <summary>
        /// Creates a transfer object for the given inner value
        /// </summary>
        /// <param name="value">The inner value</param>
        /// <param name="store">A binary store to resolve binary data</param>
        public DateDto(DateTimeOffset value, IBinaryStore store = null)
        {
            Day = (uint) value.Day;
            Month = (uint) value.Month;
            Year = (uint) value.Year;
            Timezone = value.Offset;
        }

        /// <summary>
        /// Gets or sets the day
        /// </summary>
        [ProtoMember(1, Name = @"day")]
        public uint Day { get; set; }

        /// <summary>
        /// Gets or sets the month
        /// </summary>
        [ProtoMember(2, Name = @"month")]
        public uint Month { get; set; }

        /// <summary>
        /// Gets or sets the year
        /// </summary>
        [ProtoMember(3, Name = @"year")]
        public ulong Year { get; set; }

        /// <summary>
        /// Gets or sets the timezone
        /// </summary>
        [ProtoMember(4, Name = @"timezone")]
        public TimezoneDto Timezone { get; set; }

        /// <summary>
        /// Encapsulates a given date into a data transfer object
        /// </summary>
        /// <param name="value">The actual date</param>
        public static implicit operator DateDto(DateTimeOffset value)
        {
            return new DateDto(value);
        }

        /// <summary>
        /// Extracts the value from the dto
        /// </summary>
        /// <param name="resolver">A resolver component to resolve binary data</param>
        /// <returns>The actual value</returns>
        public DateTimeOffset Extract(IBinaryStore resolver)
        {
            return new DateTimeOffset((int)Year, (int)Month, (int)Day, 0, 0, 0, Timezone.Extract(resolver));
        }

        /// <summary>
        /// Validates the contents of the current dto
        /// </summary>
        /// <returns>Null as no errors have been found</returns>
        public string GetValidationErrors()
        {
            return null;
        }

        /// <summary>
        /// Encapsulates a given date into a data transfer object
        /// </summary>
        /// <param name="value">The actual date</param>
        /// <param name="store">(ignored)</param>
        public static DateDto Create(DateTimeOffset value, IBinaryStore store = null)
        {
            return value;
        }
    }

    /// <summary>
    /// Denotes a data transfer object for a time
    /// </summary>
    [ProtoContract()]
    public class TimeDto : ISilaTransferObject<TimeSpan>
    {
        /// <summary>
        /// Creates a new data transfer object
        /// </summary>
        public TimeDto() { }

        /// <summary>
        /// Creates a transfer object for the given inner value
        /// </summary>
        /// <param name="value">The inner value</param>
        /// <param name="store">A binary store to resolve binary data</param>
        public TimeDto(TimeSpan value, IBinaryStore store = null)
        {
            Second = (uint) value.Seconds;
            Minute = (uint) value.Minutes;
            Hour = (uint) value.Hours;
            Timezone = new TimezoneDto(TimeSpan.Zero);
        }

        /// <summary>
        /// Gets or sets the second
        /// </summary>
        [ProtoMember(1, Name = @"second")]
        public uint Second { get; set; }

        /// <summary>
        /// Gets or sets the minute
        /// </summary>
        [ProtoMember(2, Name = @"minute")]
        public uint Minute { get; set; }

        /// <summary>
        /// Gets or sets the hour
        /// </summary>
        [ProtoMember(3, Name = @"hour")]
        public uint Hour { get; set; }

        /// <summary>
        /// Gets or sets the timezone
        /// </summary>
        [ProtoMember(4, Name = @"timezone")]
        public TimezoneDto Timezone { get; set; }

        /// <summary>
        /// Encapsulates the given time into a dto
        /// </summary>
        /// <param name="value">The actual time</param>
        public static implicit operator TimeDto(TimeSpan value)
        {
            return new TimeDto(value);
        }

        /// <summary>
        /// Extracts the value from the dto
        /// </summary>
        /// <param name="resolver">A resolver component to resolve binary data</param>
        /// <returns>The actual value</returns>
        public TimeSpan Extract(IBinaryStore resolver)
        {
            return new TimeSpan((int) Hour, (int) Minute, (int) Second) + Timezone.Extract(resolver);
        }

        /// <summary>
        /// Validates the contents of the current dto
        /// </summary>
        /// <returns>Null as no errors have been found</returns>
        public string GetValidationErrors()
        {
            return null;
        }

        /// <summary>
        /// Encapsulates the given time into a dto
        /// </summary>
        /// <param name="value">The actual time</param>
        /// <param name="store">(ignored)</param>
        public static TimeDto Create(TimeSpan value, IBinaryStore store = null)
        {
            return value;
        }
    }

    /// <summary>
    /// The data transfer type for a timestamp
    /// </summary>
    [ProtoContract()]
    public class TimestampDto : ISilaTransferObject<DateTimeOffset>
    {
        /// <summary>
        /// Creates an empty data transfer object
        /// </summary>
        public TimestampDto() { }

        /// <summary>
        /// Creates a transfer object for the given inner value
        /// </summary>
        /// <param name="value">The inner value</param>
        /// <param name="store">A binary store to resolve binary data</param>
        public TimestampDto(DateTimeOffset value, IBinaryStore store = null)
        {
            Second = value.Second;
            Minute = value.Minute;
            Hour = value.Hour;
            Day = value.Day;
            Month = value.Month;
            Year = value.Year;
            Timezone = value.Offset;
        }

        /// <summary>
        /// Gets or sets the second
        /// </summary>
        [ProtoMember(1, Name = @"second")]
        public long Second { get; set; }

        /// <summary>
        /// Gets or sets the minute
        /// </summary>
        [ProtoMember(2, Name = @"minute")]
        public long Minute { get; set; }

        /// <summary>
        /// Gets or sets the hour
        /// </summary>
        [ProtoMember(3, Name = @"hour")]
        public long Hour { get; set; }

        /// <summary>
        /// Gets or sets the day
        /// </summary>
        [ProtoMember(4, Name = @"day")]
        public long Day { get; set; }

        /// <summary>
        /// Gets or sets the month
        /// </summary>
        [ProtoMember(5, Name = @"month")]
        public long Month { get; set; }

        /// <summary>
        /// Gets or sets the year
        /// </summary>
        [ProtoMember(6, Name = @"year")]
        public long Year { get; set; }

        /// <summary>
        /// Gets or sets the timezone
        /// </summary>
        [ProtoMember(7, Name = @"timezone")]
        public TimezoneDto Timezone { get; set; }

        /// <summary>
        /// Encapsulates the given timestamp into a dto
        /// </summary>
        /// <param name="value">The actual timestamp</param>
        public static implicit operator TimestampDto(DateTimeOffset value)
        {
            return new TimestampDto(value);
        }

        /// <summary>
        /// Extracts the value from the dto
        /// </summary>
        /// <param name="resolver">A resolver component to resolve binary data</param>
        /// <returns>The actual value</returns>
        public DateTimeOffset Extract(IBinaryStore resolver)
        {
            return new DateTimeOffset((int)Year, (int)Month, (int)Day, (int)Hour, (int)Minute, (int)Second, Timezone.Extract(resolver));
        }

        /// <summary>
        /// Validates the contents of the current dto
        /// </summary>
        /// <returns>Null as no errors have been found</returns>
        public string GetValidationErrors()
        {
            return null;
        }

        /// <summary>
        /// Encapsulates the given timestamp into a dto
        /// </summary>
        /// <param name="value">The actual timestamp</param>
        /// <param name="store">(ignored)</param>
        public static TimestampDto Create(DateTimeOffset value, IBinaryStore store = null)
        {
            return value;
        }
    }

    /// <summary>
    /// The data transfer type for a timezone
    /// </summary>
    [ProtoContract()]
    public class TimezoneDto : ISilaTransferObject<TimeSpan>
    {
        /// <summary>
        /// Creates an empty transfer object
        /// </summary>
        public TimezoneDto() { }

        /// <summary>
        /// Creates a transfer object for the given inner value
        /// </summary>
        /// <param name="value">The inner value</param>
        /// <param name="store">A binary store to resolve binary data</param>
        public TimezoneDto(TimeSpan value, IBinaryStore store = null)
        {
            Hours = value.Hours;
            Minutes = (uint) value.Minutes;
        }

        /// <summary>
        /// Gets or sets the offset hours
        /// </summary>
        [ProtoMember(1, Name = @"hours")]
        public int Hours { get; set; }

        /// <summary>
        /// Gets or sets the offset minutes
        /// </summary>
        [ProtoMember(2, Name = @"minutes")]
        public uint Minutes { get; set; }

        /// <summary>
        /// Wraps the given offset into a transfer object
        /// </summary>
        /// <param name="offset">The given offset</param>
        public static implicit operator TimezoneDto(TimeSpan offset)
        {
            return new TimezoneDto(offset);
        }

        /// <summary>
        /// Extracts the value from the dto
        /// </summary>
        /// <param name="resolver">A resolver component to resolve binary data</param>
        /// <returns>The actual value</returns>
        public TimeSpan Extract(IBinaryStore resolver)
        {
            return new TimeSpan(0, Hours, (int)Minutes, 0);
        }

        /// <summary>
        /// Validates the contents of the current dto
        /// </summary>
        /// <returns>Null as no errors have been found</returns>
        public string GetValidationErrors()
        {
            return null;
        }

        /// <summary>
        /// Wraps the given offset into a transfer object
        /// </summary>
        /// <param name="value">The given offset</param>
        public static TimezoneDto Create(TimeSpan value)
        {
            return new TimezoneDto(value);
        }
    }

    /// <summary>
    /// The data transfer type for a duration
    /// </summary>
    [ProtoContract()]
    public class DurationDto : ISilaTransferObject<TimeSpan>
    {
        /// <summary>
        /// Creates an empty transfer object
        /// </summary>
        public DurationDto() { }

        /// <summary>
        /// Creates a transfer object for the given inner value
        /// </summary>
        /// <param name="value">The inner value</param>
        /// <param name="store">A binary store to resolve binary data</param>
        public DurationDto(TimeSpan value, IBinaryStore store = null)
        {
            Nanos = (int)(value.Ticks % TimeSpan.TicksPerSecond) * 100;
            Seconds = (long)value.TotalSeconds;
        }

        /// <summary>
        /// Gets or sets the seconds
        /// </summary>
        [ProtoMember(1, Name = @"seconds")]
        public long Seconds { get; set; }

        /// <summary>
        /// Gets or sets the nanoseconds
        /// </summary>
        [ProtoMember(2, Name = @"nanos")]
        public int Nanos { get; set; }

        /// <summary>
        /// Encapsulates the given duration into a transfer object
        /// </summary>
        /// <param name="value">The actual duration</param>
        public static implicit operator DurationDto(TimeSpan value)
        {
            return new DurationDto(value);
        }

        /// <summary>
        /// Extracts the value from the dto
        /// </summary>
        /// <param name="resolver">A resolver component to resolve binary data</param>
        /// <returns>The actual value</returns>
        public TimeSpan Extract(IBinaryStore resolver)
        {
            return new TimeSpan(Seconds * TimeSpan.TicksPerSecond + (Nanos / 100));
        }

        /// <summary>
        /// Validates the contents of the current dto
        /// </summary>
        /// <returns>Null as no errors have been found</returns>
        public string GetValidationErrors()
        {
            return null;
        }
    }

    /// <summary>
    /// The data transfer class for a command execution identifier
    /// </summary>
    [ProtoContract(Name = "CommandExecutionUUID")]
    public class CommandExecutionUuid
    {
        /// <summary>
        /// Creates an empty data transfer object
        /// </summary>
        public CommandExecutionUuid() { }

        /// <summary>
        /// Creates a data transfer object for the given command execution identifier
        /// </summary>
        /// <param name="commandId"></param>
        public CommandExecutionUuid(string commandId)
        {
            CommandId = commandId;
        }

        /// <summary>
        /// Gets the command execution identifier
        /// </summary>
        [ProtoMember(1, Name = "commandId")]
        [DefaultValue("")]
        public string CommandId { get; set; } = "";

    }

    /// <summary>
    /// The data transfer object for a command confirmation
    /// </summary>
    [ProtoContract()]
    public class CommandConfirmation
    {
        /// <summary>
        /// Creates an empty data transfer object
        /// </summary>
        public CommandConfirmation()
        {
        }

        /// <summary>
        /// Creates a data transfer object for the given command execution identifier
        /// </summary>
        /// <param name="commandId">The command execution identifier</param>
        public CommandConfirmation(string commandId)
        {
            CommandId = new CommandExecutionUuid(commandId);
        }
        
        /// <summary>
        /// Creates a data transfer object for the given command execution identifier
        /// </summary>
        /// <param name="commandId">The command execution identifier</param>
        /// <param name="expectedDuration">The expected duration</param>
        public CommandConfirmation(string commandId, TimeSpan expectedDuration)
        {
            CommandId = new CommandExecutionUuid(commandId);
            LifetimeOfExecution = new DurationDto(expectedDuration);
        }

        /// <summary>
        /// Gets the command execution identifier
        /// </summary>
        [ProtoMember(1, Name = "commandExecutionUUID")]
        public CommandExecutionUuid CommandId { get; set; }

        /// <summary>
        /// Gets the expected lifetime of execution
        /// </summary>
        [ProtoMember(2, Name = "lifetimeOfExecution")]
        public DurationDto LifetimeOfExecution { get; set; }

    }

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
        public ExecutionState(CommandStatus status, double progress, TimeSpan estimatedRemainingTime)
        {
            Status = status;
            Progress = progress;
            EstimatedRemainingTime = estimatedRemainingTime;
        }

        /// <summary>
        /// Gets or sets the command state
        /// </summary>
        [ProtoMember(1, Name = "commandStatus")]
        public CommandStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the progress
        /// </summary>
        [ProtoMember(2, Name = @"progress")]
        public RealDto Progress { get; set; }

        /// <summary>
        /// Gets or sets the estimated remaining time
        /// </summary>
        [ProtoMember(3, Name = "estimatedRemainingTime")]
        public DurationDto EstimatedRemainingTime { get; set; }

        /// <summary>
        /// Gets or sets the updated lifetime of execution
        /// </summary>
        [ProtoMember(4, Name = "updatedLifetimeOfExecution")]
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
            [ProtoEnum(Name = @"waiting")]
            Waiting = 0,
            /// <summary>
            /// The command is running
            /// </summary>
            [ProtoEnum(Name = @"running")]
            Running = 1,
            /// <summary>
            /// The command finished successfully
            /// </summary>
            [ProtoEnum(Name = @"finishedSuccessfully")]
            FinishedSuccessfully = 2,
            /// <summary>
            /// The command finished with error
            /// </summary>
            [ProtoEnum(Name = @"finishedWithError")]
            FinishedWithError = 3,
        }

    }

    /// <summary>
    /// The data transfer object for an error
    /// </summary>
    [ProtoContract()]
    public class SiLAErrorDto
    {
        /// <summary>
        /// Gets or sets the validation error
        /// </summary>
        [ProtoMember(1)]
        public ValidationErrorDto ValidationError
        {
            get { return __pbn__error.Is(1) ? ((ValidationErrorDto)__pbn__error.Object) : default; }
            set { __pbn__error = new DiscriminatedUnionObject(1, value); }
        }
        /// <summary>
        /// Gets a value indicating whether the validation error should be serialized
        /// </summary>
        /// <returns>True, if the validation error should be serialized, otherwise False</returns>
        public bool ShouldSerializeValidationError() => __pbn__error.Is(1);
        /// <summary>
        /// Resets the validation error property
        /// </summary>
        public void ResetValidationError() => DiscriminatedUnionObject.Reset(ref __pbn__error, 1);

        private DiscriminatedUnionObject __pbn__error;

        /// <summary>
        /// Gets or sets the execution error
        /// </summary>
        [ProtoMember(2)]
        public DefinedExecutionErrorDto DefinedExecutionError
        {
            get { return __pbn__error.Is(2) ? ((DefinedExecutionErrorDto)__pbn__error.Object) : default; }
            set { __pbn__error = new DiscriminatedUnionObject(2, value); }
        }
        /// <summary>
        /// Gets a value indicating whether the execution error should be serialized
        /// </summary>
        /// <returns>True, if the execution error should be serialized, otherwise False</returns>
        public bool ShouldSerializeDefinedExecutionError() => __pbn__error.Is(2);
        /// <summary>
        /// Resets the execution error property
        /// </summary>
        public void ResetDefinedExecutionError() => DiscriminatedUnionObject.Reset(ref __pbn__error, 2);

        /// <summary>
        /// Gets or sets the undefined execution error
        /// </summary>
        [ProtoMember(3)]
        public UndefinedExecutionErrorDto UndefinedExecutionError
        {
            get { return __pbn__error.Is(3) ? ((UndefinedExecutionErrorDto)__pbn__error.Object) : default; }
            set { __pbn__error = new DiscriminatedUnionObject(3, value); }
        }
        /// <summary>
        /// Gets a value indicating whether the undefined execution error should be serialized
        /// </summary>
        /// <returns>True, if the undefined execution error should be serialized, otherwise False</returns>
        public bool ShouldSerializeUndefinedExecutionError() => __pbn__error.Is(3);
        /// <summary>
        /// Resets the undefined execution error property
        /// </summary>
        public void ResetUndefinedExecutionError() => DiscriminatedUnionObject.Reset(ref __pbn__error, 3);

        /// <summary>
        /// Gets or sets the framework error
        /// </summary>
        [ProtoMember(4)]
        public FrameworkErrorDto FrameworkError
        {
            get { return __pbn__error.Is(4) ? ((FrameworkErrorDto)__pbn__error.Object) : default; }
            set { __pbn__error = new DiscriminatedUnionObject(4, value); }
        }
        /// <summary>
        /// Gets a value indicating whether the framework error should be serialized
        /// </summary>
        /// <returns>True, if the framework error should be serialized, otherwise False</returns>
        public bool ShouldSerializeFrameworkError() => __pbn__error.Is(4);
        /// <summary>
        /// Resets the framework error property
        /// </summary>
        public void ResetFrameworkError() => DiscriminatedUnionObject.Reset(ref __pbn__error, 4);

    }

    /// <summary>
    /// The data transfer object for a validation error
    /// </summary>
    [ProtoContract()]
    public class ValidationErrorDto
    {
        /// <summary>
        /// Gets or sets the parameter identifier that did not validate
        /// </summary>
        [ProtoMember(1, Name = @"parameter")]
        [DefaultValue("")]
        public string Parameter { get; set; } = "";

        /// <summary>
        /// Gets or sets the error message
        /// </summary>
        [ProtoMember(2, Name = @"message")]
        [DefaultValue("")]
        public string Message { get; set; } = "";

    }

    /// <summary>
    /// The data transfer object for an execution error
    /// </summary>
    [ProtoContract()]
    public class DefinedExecutionErrorDto
    {
        /// <summary>
        /// Gets or sets the fully qualified identifier of the execution error
        /// </summary>
        [ProtoMember(1)]
        [DefaultValue("")]
        public string ErrorIdentifier { get; set; } = "";

        /// <summary>
        /// Gets or sets the error message
        /// </summary>
        [ProtoMember(2, Name = @"message")]
        [DefaultValue("")]
        public string Message { get; set; } = "";

    }

    /// <summary>
    /// The data transfer object for an undefined execution error
    /// </summary>
    [ProtoContract()]
    public class UndefinedExecutionErrorDto : IExtensible
    {
        private IExtension __pbn__extensionData;
        IExtension IExtensible.GetExtensionObject(bool createIfMissing)
            => Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        /// <summary>
        /// Gets or sets the error message
        /// </summary>
        [ProtoMember(1, Name = @"message")]
        [DefaultValue("")]
        public string Message { get; set; } = "";

    }

    /// <summary>
    /// The data transfer type for a framework error
    /// </summary>
    [ProtoContract()]
    public class FrameworkErrorDto : IExtensible
    {
        private IExtension __pbn__extensionData;
        IExtension IExtensible.GetExtensionObject(bool createIfMissing)
            => Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        /// <summary>
        /// Gets or sets the type of error
        /// </summary>
        [ProtoMember(1)]
        public FrameworkErrorType Type { get; set; }

        /// <summary>
        /// Gets or sets the error message
        /// </summary>
        [ProtoMember(2, Name = @"message")]
        [DefaultValue("")]
        public string Message { get; set; } = "";


    }

    /// <summary>
    /// Denotes the framework error type
    /// </summary>
    [ProtoContract()]
    public enum FrameworkErrorType
    {
        /// <summary>
        /// The server denied the execution of a command
        /// </summary>
        [ProtoEnum(Name = @"COMMAND_EXECUTION_NOT_ACCEPTED")]
        CommandExecutionNotAccepted = 0,
        /// <summary>
        /// The execution identifier is invalid
        /// </summary>
        [ProtoEnum(Name = @"INVALID_COMMAND_EXECUTION_UUID")]
        InvalidCommandExecutionUuid = 1,
        /// <summary>
        /// The command execution did not finish
        /// </summary>
        [ProtoEnum(Name = @"COMMAND_EXECUTION_NOT_FINISHED")]
        CommandExecutionNotFinished = 2,
        /// <summary>
        /// The metadata provided is invalid
        /// </summary>
        [ProtoEnum(Name = @"INVALID_METADATA")]
        InvalidMetadata = 3,
        /// <summary>
        /// No metadata is allowed for this request
        /// </summary>
        [ProtoEnum(Name = @"NO_METADATA_ALLOWED")]
        NoMetadataAllowed = 4,
    }

    /// <summary>
    /// Helper class with extensions for Dtos
    /// </summary>
    public static class DtoExtensions
    {
        /// <summary>
        /// Extracts the collection of values in the collection of data transfer objects
        /// </summary>
        /// <typeparam name="T">The type of objects</typeparam>
        /// <param name="source">A collection with data transfer objects</param>
        /// <param name="resolver">A component that resolves binary storage identifiers</param>
        /// <returns>A collection with the underlying data</returns>
        public static ICollection<T> Extract<T>(this IEnumerable<ISilaTransferObject<T>> source, IBinaryStore resolver)
        {
            return source?.Select(it => it.Extract(resolver)).ToList();
        }

        /// <summary>
        /// Encapsulates the given collection into data transfer objects
        /// </summary>
        /// <typeparam name="TDto">The type of data transfer objects</typeparam>
        /// <typeparam name="TInner">The type of data</typeparam>
        /// <param name="source">The collection of data objects</param>
        /// <param name="convertFunc">A conversion function to encapsulate the data</param>
        /// <param name="resolver">A component to handle binary data transfer</param>
        /// <returns></returns>
        public static List<TDto> Encapsulate<TDto, TInner>(IEnumerable<TInner> source,
            Func<TInner, IBinaryStore, TDto> convertFunc, IBinaryStore resolver)
        {
            return source?.Select(item => convertFunc(item, resolver)).ToList();
        }
    }
}
