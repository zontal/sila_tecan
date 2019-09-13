using System;

namespace Tecan.Sila2
{
    /// <summary>
    /// Event arguments if an observable command emits intermediate results
    /// </summary>
    /// <typeparam name="T">The type of intermediate results</typeparam>
    public class IntermediateResponseEventArgs<T> : EventArgs
    {
        /// <summary>
        /// Creates new intermediate results event arguments for the given data
        /// </summary>
        /// <param name="data">The actual data</param>
        public IntermediateResponseEventArgs(T data)
        {
            Data = data;
        }

        /// <summary>
        /// The given intermediate results
        /// </summary>
        public T Data { get; }
    }
}
