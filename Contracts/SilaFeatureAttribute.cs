using System;

namespace Tecan.Sila2
{
    /// <summary>
    /// Specifies that a given interface should be the basis for a SiLA feature
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface)]
    public class SilaFeatureAttribute : Attribute
    {
        /// <summary>
        /// Gets whether the given feature is a draft specification
        /// </summary>
        public bool IsDraft { get; }

        /// <summary>
        /// Gets the use case name for the feature representing the annotated interface
        /// </summary>
        public string Category { get; }

        /// <summary>
        /// Creates a feature annotation as draft without a predefined use case
        /// </summary>
        public SilaFeatureAttribute() : this(true, null) { }

        /// <summary>
        /// Creates a feature annotation as draft with the given use case
        /// </summary>
        /// <param name="useCase">The use case of the feature</param>
        public SilaFeatureAttribute(string useCase) : this(true, useCase) { }

        /// <summary>
        /// Creates a feature annotation
        /// </summary>
        /// <param name="isDraft">indicates whether the feature is draft</param>
        /// <param name="category">the use case of the feature</param>
        public SilaFeatureAttribute(bool isDraft, string category)
        {
            IsDraft = isDraft;
            Category = category;
        }
    }
}
