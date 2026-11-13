
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Sang.Service.TallyIntegration.Models;
using Sang.Service.TallyIntegration.Configurations;

namespace Sang.Service.TallyIntegration.Builders
{
    public class TallyMasterXmlBuilder : ITallyMasterBuilder
    {
        public string Build(TallyMasterRequest request)
        {
            ValidateRequest(request);

            var definition = TallyMasterRegistry.Get(request.MasterType);

            // Create master element.
            var master = new XElement(
                definition.XmlElementName,
                new XAttribute("NAME", request.Master.Name.Trim()),
                new XAttribute("ACTION", definition.XmlAction));

            var hasCode = !string.IsNullOrWhiteSpace(request.Master.Code);

            var codeAsAlias = definition.XmlCodeElement.Equals(
                "ALIAS",
                StringComparison.OrdinalIgnoreCase);

            // Handle master name and code.
            if (hasCode && codeAsAlias)
            {
                // Store code as an alias.
                master.Add(
                    new XElement("LANGUAGENAME.LIST",
                        new XElement("NAME.LIST",
                            new XAttribute("TYPE", "String"),
                            new XElement("NAME", request.Master.Name.Trim()),
                            new XElement("NAME", request.Master.Code.Trim())
                        )
                    )
                );
            }
            else
            {
                master.Add(
                    new XElement("NAME", request.Master.Name.Trim()));

                if (hasCode)
                {
                    master.Add(
                        new XElement(
                            definition.XmlCodeElement,
                            request.Master.Code.Trim()));
                }
            }

            // Handle parent.
            // Primary is treated as the top-level parent.
            if (!string.IsNullOrWhiteSpace(request.Master.Parent) &&
                !request.Master.Parent.Trim().Equals(
                    "Primary",
                    StringComparison.OrdinalIgnoreCase))
            {
                master.Add(
                    new XElement("PARENT", request.Master.Parent.Trim()));
            }

            // Handle dynamic extra fields.
            if (request.Master.ExtraFields != null)
            {
                foreach (var field in request.Master.ExtraFields)
                {
                    ValidateField(field.Key, definition);

                    master.Add(
                        CreateXmlElement(
                            field.Key.Trim().ToUpperInvariant(),
                            field.Value));
                }
            }

            // Build static variables.
            var staticVariables = new XElement("STATICVARIABLES");

            // Company selection is optional.
            // If empty, Tally uses the currently open company.
            if (!string.IsNullOrWhiteSpace(request.CompanyName))
            {
                staticVariables.Add(
                    new XElement(
                        "SVCURRENTCOMPANY",
                        request.CompanyName.Trim()));
            }

            // Build final Tally import XML.
            var xml = new XDocument(
                new XElement("ENVELOPE",

                    new XElement("HEADER",
                        new XElement("VERSION", "1"),
                        new XElement("TALLYREQUEST", "Import"),
                        new XElement("TYPE", "Data"),
                        new XElement("ID", "All Masters")
                    ),

                    new XElement("BODY",

                        new XElement("DESC",
                            staticVariables
                        ),

                        new XElement("DATA",
                            new XElement("TALLYMESSAGE",
                                new XAttribute(
                                    XNamespace.Xmlns + "UDF",
                                    "TallyUDF"),
                                master
                            )
                        )
                    )
                )
            );

            return xml.ToString();
        }

        // =========================================================
        // CREATE XML ELEMENT FROM DYNAMIC JSON VALUE
        // =========================================================

        private static XElement CreateXmlElement(
            string name,
            object? value)
        {
            XmlConvert.VerifyName(name);

            if (value == null)
                return new XElement(name);

            var json = JsonSerializer.SerializeToElement(value);

            return ConvertJsonElement(name, json);
        }

        // =========================================================
        // CONVERT JSON ELEMENT TO XML
        // =========================================================

        private static XElement ConvertJsonElement(
            string name,
            JsonElement value)
        {
            return value.ValueKind switch
            {
                JsonValueKind.Object => new XElement(
                    name,
                    value.EnumerateObject().Select(property =>
                        ConvertJsonElement(
                            property.Name.ToUpperInvariant(),
                            property.Value))),

                JsonValueKind.Array => new XElement(
                    name,
                    value.EnumerateArray().Select(item =>
                        ConvertJsonElement("ITEM", item))),

                JsonValueKind.Null => new XElement(name),

                JsonValueKind.True => new XElement(name, "Yes"),

                JsonValueKind.False => new XElement(name, "No"),

                _ => new XElement(name, value.ToString())
            };
        }

        // =========================================================
        // VALIDATE MASTER REQUEST
        // =========================================================

        private static void ValidateRequest(
            TallyMasterRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            // CompanyName is intentionally optional.
            // If provided, it will be added as SVCURRENTCOMPANY.

            if (string.IsNullOrWhiteSpace(request.MasterType))
            {
                throw new ArgumentException(
                    "MasterType is required.");
            }

            if (request.Master == null)
            {
                throw new ArgumentException(
                    "Master details are required.");
            }

            if (string.IsNullOrWhiteSpace(request.Master.Name))
            {
                throw new ArgumentException(
                    "Master name is required.");
            }

            request.Master.ExtraFields ??=
                new Dictionary<string, object?>();
        }

        // =========================================================
        // VALIDATE EXTRA FIELD
        // =========================================================

        private static void ValidateField(
            string fieldName,
            TallyMasterDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                throw new ArgumentException(
                    "Extra field name is required.");
            }

            var reservedFields = new[]
            {
                "NAME",
                "PARENT",
                definition.XmlCodeElement
            };

            if (reservedFields.Contains(
                fieldName,
                StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"Extra field '{fieldName}' is reserved.");
            }

            XmlConvert.VerifyName(
                fieldName.Trim().ToUpperInvariant());
        }
    }
}
