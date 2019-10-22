using Tecan.Sila2.Binary;

namespace Tecan.Sila2
{
    /// <summary>
    /// Defines a class used as data transfer object type
    /// </summary>
    public interface ISilaTransferObject
    {
        /// <summary>
        /// Checks whether there are any validation errors
        /// </summary>
        /// <returns>Null if there are no validation errors, otherwise a validation error message</returns>
        string GetValidationErrors();
    }

    /// <summary>
    /// Denotes a data transfer object for a given carried type
    /// </summary>
    /// <typeparam name="T">The inner value type</typeparam>
    public interface ISilaTransferObject<out T> : ISilaTransferObject
    {
        /// <summary>
        /// Extracts the transferred inner value
        /// </summary>
        /// <param name="resolver">A binary store to resolve binary storage identifiers</param>
        /// <returns>The inner value</returns>
        T Extract(IBinaryStore resolver);
    }
}
