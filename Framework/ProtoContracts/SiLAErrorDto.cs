using ProtoBuf;

namespace Tecan.Sila2
{
    /// <summary>
    /// The data transfer object for an error
    /// </summary>
    [ProtoContract()]
    public class SiLAErrorDto
    {
        /// <summary>
        /// Gets or sets the validation error
        /// </summary>
        [ProtoMember( 1 )]
        public ValidationErrorDto ValidationError
        {
            get { return __pbn__error.Is( 1 ) ? ((ValidationErrorDto)__pbn__error.Object) : default; }
            set { __pbn__error = new DiscriminatedUnionObject( 1, value ); }
        }
        /// <summary>
        /// Gets a value indicating whether the validation error should be serialized
        /// </summary>
        /// <returns>True, if the validation error should be serialized, otherwise False</returns>
        public bool ShouldSerializeValidationError() => __pbn__error.Is( 1 );
        /// <summary>
        /// Resets the validation error property
        /// </summary>
        public void ResetValidationError() => DiscriminatedUnionObject.Reset( ref __pbn__error, 1 );

        private DiscriminatedUnionObject __pbn__error;

        /// <summary>
        /// Gets or sets the execution error
        /// </summary>
        [ProtoMember( 2 )]
        public DefinedExecutionErrorDto DefinedExecutionError
        {
            get { return __pbn__error.Is( 2 ) ? ((DefinedExecutionErrorDto)__pbn__error.Object) : default; }
            set { __pbn__error = new DiscriminatedUnionObject( 2, value ); }
        }
        /// <summary>
        /// Gets a value indicating whether the execution error should be serialized
        /// </summary>
        /// <returns>True, if the execution error should be serialized, otherwise False</returns>
        public bool ShouldSerializeDefinedExecutionError() => __pbn__error.Is( 2 );
        /// <summary>
        /// Resets the execution error property
        /// </summary>
        public void ResetDefinedExecutionError() => DiscriminatedUnionObject.Reset( ref __pbn__error, 2 );

        /// <summary>
        /// Gets or sets the undefined execution error
        /// </summary>
        [ProtoMember( 3 )]
        public UndefinedExecutionErrorDto UndefinedExecutionError
        {
            get { return __pbn__error.Is( 3 ) ? ((UndefinedExecutionErrorDto)__pbn__error.Object) : default; }
            set { __pbn__error = new DiscriminatedUnionObject( 3, value ); }
        }
        /// <summary>
        /// Gets a value indicating whether the undefined execution error should be serialized
        /// </summary>
        /// <returns>True, if the undefined execution error should be serialized, otherwise False</returns>
        public bool ShouldSerializeUndefinedExecutionError() => __pbn__error.Is( 3 );
        /// <summary>
        /// Resets the undefined execution error property
        /// </summary>
        public void ResetUndefinedExecutionError() => DiscriminatedUnionObject.Reset( ref __pbn__error, 3 );

        /// <summary>
        /// Gets or sets the framework error
        /// </summary>
        [ProtoMember( 4 )]
        public FrameworkErrorDto FrameworkError
        {
            get { return __pbn__error.Is( 4 ) ? ((FrameworkErrorDto)__pbn__error.Object) : default; }
            set { __pbn__error = new DiscriminatedUnionObject( 4, value ); }
        }
        /// <summary>
        /// Gets a value indicating whether the framework error should be serialized
        /// </summary>
        /// <returns>True, if the framework error should be serialized, otherwise False</returns>
        public bool ShouldSerializeFrameworkError() => __pbn__error.Is( 4 );
        /// <summary>
        /// Resets the framework error property
        /// </summary>
        public void ResetFrameworkError() => DiscriminatedUnionObject.Reset( ref __pbn__error, 4 );

    }
}
