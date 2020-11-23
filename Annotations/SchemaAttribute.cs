using System;

namespace Tecan.Sila2
{
    /// <summary>
    /// Defines that the value of the given property or class has to conform to a given schema
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Struct | AttributeTargets.Property)]
    public class SchemaAttribute : Attribute
    {
        /// <summary>
        /// The type of the schema
        /// </summary>
        public SchemaType Type { get; set; }

        /// <summary>
        /// A URI to the schema document
        /// </summary>
        public string Schema { get; set; }

        /// <summary>
        /// Creates a new instance for an XML schema
        /// </summary>
        /// <param name="schema">The URI of the XML schema</param>
        public SchemaAttribute(string schema) : this(schema, SchemaType.Xml) { }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="schema">The URI of the schema</param>
        /// <param name="type">The type of the schema</param>
        public SchemaAttribute(string schema, SchemaType type)
        {
            Schema = schema;
            Type = type;
        }
    }
}
