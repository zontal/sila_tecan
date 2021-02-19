using System;

namespace Tecan.Sila2.ParameterConstraintsProvider
{
    /// <summary>
    /// Defines that a parameter is constrained to the file names that exist in a given folder
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class FileConstraintAttribute : Attribute
    {

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="folder">The folder where to look</param>
        public FileConstraintAttribute( string folder )
        {
            Folder = folder;
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="folder">The folder where to look</param>
        /// <param name="filter">A filter expression of files</param>
        public FileConstraintAttribute( string folder, string filter )
        {
            Folder = folder;
            Filter = filter ?? Filter;
        }

        /// <summary>
        /// The folder where to look
        /// </summary>
        public string Folder
        {
            get;
        }

        /// <summary>
        /// A filter expression of files, defaults to *.*
        /// </summary>
        public string Filter
        {
            get;
        } = "*.*";
    }
}
