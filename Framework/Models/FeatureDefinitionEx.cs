using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2
{
    /// <summary>
    /// This class extents the Feature class auto-generated from the FeatureDefinition schema.
    /// </summary>
    public partial class Feature
    {
        /// <summary>
        /// Gets the fully qualified identifier assembled from several feature properties as specified in the SiLA 2 standard.
        /// </summary>
        public string FullyQualifiedIdentifier => $"{this.Originator}/{(this.Category != null ? this.Category + "/" : string.Empty)}{this.Identifier}/v{Version.Parse(this.FeatureVersion).Major}";


        /// <summary>
        /// Gets the namespace to be used when calling the gRPC service.
        /// </summary>
        public string Namespace => $"sila2.{Originator}.{Category}.{Identifier.ToLower()}.v{Version.Parse(this.FeatureVersion).Major}";


        public string GetFullyQualifiedIdentifier(FeatureCommand command)
        {
            return FullyQualifiedIdentifier + "/Command/" + command.Identifier;
        }

        public string GetFullyQualifiedIdentifier(FeatureProperty proprty)
        {
            return FullyQualifiedIdentifier + "/Property/" + proprty.Identifier;
        }

        public string GetFullyQualifiedIdentifier(FeatureMetadata metadata)
        {
            return FullyQualifiedIdentifier + "/Metadata/" + metadata.Identifier;
        }

        public string GetFullyQualifiedIdentifier(FeatureDefinedExecutionError error)
        {
            return FullyQualifiedIdentifier + "/DefinedError/" + error.Identifier;
        }

        #region Overrides of Object

        /// <summary>
        /// Lists the Featrue Definition content in a readable multiline manner.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var property in this.GetType().GetProperties())
            {
                switch (property.Name)
                {
                    case "Items":
                        break;

                    default:
                        sb.Append(property.Name);

                        sb.Append(": ");
                        if (property.GetIndexParameters().Length > 0)
                        {
                            sb.Append("Indexed Property cannot be used");
                        }
                        else
                        {
                            sb.Append(property.GetValue(this, null));
                        }

                        sb.Append(Environment.NewLine);
                        break;
                }
            }

            // items
            sb.Append(Environment.NewLine);
            foreach (var item in this.Items)
            {
                if (item is FeatureCommand command)
                {
                    sb.AppendFormat("<Command> \"{0}\": {1}\n", command.Identifier, command.Description);
                    if (command.Parameter != null)
                    {
                        sb.AppendLine("   Parameters:");
                        foreach (var parameter in command.Parameter)
                        {
                            sb.AppendFormat("   - \"{0}\" [{1}]: {2}\n", parameter.Identifier, GetDataTypeString(parameter.DataType), parameter.Description);
                        }
                    }

                    if (command.Response != null)
                    {
                        sb.AppendLine("   Responses:");
                        foreach (var response in command.Response)
                        {
                            sb.AppendFormat("   - \"{0}\" [{1}]: {2}\n", response.Identifier, GetDataTypeString(response.DataType), response.Description);
                        }
                    }

                    if (command.IntermediateResponse != null)
                    {
                        sb.AppendLine("   Intermediate Responses:");
                        foreach (var intermediateResponse in command.IntermediateResponse)
                        {
                            sb.AppendFormat("   - \"{0}\" [{1}]: {2}\n", intermediateResponse.Identifier, GetDataTypeString(intermediateResponse.DataType), intermediateResponse.Description);
                        }
                    }

                    if (command.DefinedExecutionErrors != null)
                    {
                        sb.AppendLine("   Standard Execution Errors:");
                        foreach (var error in command.DefinedExecutionErrors)
                        {
                            sb.AppendFormat("   - \"{0}\"\n", error);
                        }
                    }

                }
                else if (item is FeatureDefinedExecutionError error)
                {
                    sb.AppendFormat("<StandardExecutionError> \"{0}\": {1}\n", error.Identifier, error.Description);
                }
                else if (item is FeatureProperty property)
                {
                    sb.AppendFormat("<Property> \"{0}\" {1} [{2}]: {3}\n", property.Identifier, property.Observable, GetDataTypeString(property.DataType), property.Description);
                }
                else if (item is SiLAElement dataType)
                {
                    sb.AppendFormat("<DataTypeDefinition> \"{0}\" [{1}]: {2}\n", dataType.Identifier, GetDataTypeString(dataType.DataType), dataType.Description);
                }
            }

            return sb.ToString();
        }

        private static string GetDataTypeString(DataTypeType type)
        {
            if (type.Item is BasicType)
            {
                // basic type
                return type.Item.ToString();
            }

            if (type.Item is ConstrainedType)
            {
                // constrained type
                return $"constrained {GetDataTypeString(((ConstrainedType) type.Item).DataType)}";
            }

            if (type.Item is StructureType structureType)
            {
                // structure type
                return $"structured {structureType.Element}";
            }

            if (type.Item is ListType listType)
            {
                // list type
                return $"list of {GetDataTypeString(listType.DataType)}";
            }

            // custom type
            return type.Item.ToString();
        }

        #endregion
    }
}