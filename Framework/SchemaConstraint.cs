using Common.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;

namespace Tecan.Sila2
{
    /// <summary>
    /// Utility class to check schema constraints
    /// </summary>
    public static class SchemaConstraint
    {
        private static ILog _loggingChannel = LogManager.GetLogger( typeof( SchemaConstraint ) );

        /// <summary>
        /// Determines whether checks of schema constraints are enabled or not
        /// </summary>
        public static bool IsEnabled
        {
            get;
            set;
        } = true;

        /// <summary>
        /// Checks the given parameter value whether it complies with the given schema
        /// </summary>
        /// <param name="parameter">The parameter value</param>
        /// <param name="schemaType">The type of the schema</param>
        /// <param name="schema">The schema reference</param>
        /// <param name="parameterName">The name of the parameter</param>
        /// <exception cref="ArgumentNullException">Thrown if the parameter is null</exception>
        /// <exception cref="ArgumentException">Thrown if the parameter is not compliant with the schema</exception>
        public static void Check( string parameter, SchemaType schemaType, string schema, string parameterName )
        {
            Check( new StringReader( parameter ?? throw new ArgumentNullException( parameterName ) ), schemaType, schema, parameterName );
        }

        /// <summary>
        /// Checks the given parameter value whether it complies with the given schema
        /// </summary>
        /// <param name="parameter">The parameter value</param>
        /// <param name="schemaType">The type of the schema</param>
        /// <param name="schema">The schema reference</param>
        /// <param name="parameterName">The name of the parameter</param>
        /// <exception cref="ArgumentNullException">Thrown if the parameter is null</exception>
        /// <exception cref="ArgumentException">Thrown if the parameter is not compliant with the schema</exception>
        public static void Check( Stream parameter, SchemaType schemaType, string schema, string parameterName )
        {
            Check( new StreamReader( parameter ?? throw new ArgumentNullException( parameterName ) ), schemaType, schema, parameterName );
        }

        /// <summary>
        /// Checks the given parameter value whether it complies with the given schema
        /// </summary>
        /// <param name="parameter">The parameter value</param>
        /// <param name="schemaType">The type of the schema</param>
        /// <param name="schema">The schema reference</param>
        /// <param name="parameterName">The name of the parameter</param>
        /// <exception cref="ArgumentNullException">Thrown if the parameter is null</exception>
        /// <exception cref="ArgumentException">Thrown if the parameter is not compliant with the schema</exception>
        public static void Check( TextReader parameter, SchemaType schemaType, string schema, string parameterName )
        {
            var exception = IsValid( parameter, schemaType, schema );
            if(exception != null)
            {
                throw new ArgumentException( $"Value does not comply with schema {schema}: {exception.Message}", parameterName, exception );
            }
        }

        /// <summary>
        /// Checks the given parameter value whether it complies with the given schema
        /// </summary>
        /// <param name="parameter">The parameter value</param>
        /// <param name="schemaType">The type of the schema</param>
        /// <param name="schema">The schema reference</param>
        /// <exception cref="ArgumentNullException">Thrown if the parameter is null</exception>
        /// <returns>Null, if the schema constraint is satisfied, otherwise the first exception</returns>
        public static Exception IsValid( string parameter, SchemaType schemaType, string schema )
        {
            return IsValid( new StringReader( parameter ?? throw new ArgumentNullException( nameof( parameter ) ) ), schemaType, schema );
        }

        /// <summary>
        /// Checks the given parameter value whether it complies with the given schema
        /// </summary>
        /// <param name="parameter">The parameter value</param>
        /// <param name="schemaType">The type of the schema</param>
        /// <param name="schema">The schema reference</param>
        /// <exception cref="ArgumentNullException">Thrown if the parameter is null</exception>
        /// <returns>Null, if the schema constraint is satisfied, otherwise the first exception</returns>
        public static Exception IsValid( Stream parameter, SchemaType schemaType, string schema )
        {
            return IsValid( new StreamReader( parameter ?? throw new ArgumentNullException( nameof( parameter ) ) ), schemaType, schema );
        }

        /// <summary>
        /// Checks the given parameter value whether it complies with the given schema
        /// </summary>
        /// <param name="parameter">The parameter value</param>
        /// <param name="schemaType">The type of the schema</param>
        /// <param name="schema">The schema reference</param>
        /// <exception cref="ArgumentNullException">Thrown if the parameter is null</exception>
        /// <returns>Null, if the schema constraint is satisfied, otherwise the first exception</returns>
        public static Exception IsValid( TextReader parameter, SchemaType schemaType, string schema )
        {
            if (!IsEnabled)
            {
                _loggingChannel.Warn("Skipping schema constraint check because it is explicitly disabled.");
                return null;
            }

            if(schemaType != SchemaType.Xml)
            {
                _loggingChannel.Warn( $"Validating schemas of type {schemaType} is not supported. Bypassing schema validation." );
                return null;
            }

            var exception = (Exception)null;

            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
                DtdProcessing = DtdProcessing.Ignore,
                CloseInput = true,
            };
            settings.Schemas.Add( schema, schema );
            settings.ValidationEventHandler += ( o, e ) =>
            {
                if(e.Severity == XmlSeverityType.Warning)
                {
                    _loggingChannel.Warn( e.Message );
                }
                else
                {
                    _loggingChannel.Error( e.Message );
                    exception ??= e.Exception;
                }
            };

            var reader = XmlReader.Create( parameter, settings );

            while(reader.Read()) { }

            return exception;
        }
    }
}
