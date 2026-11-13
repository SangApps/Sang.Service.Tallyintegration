
using System.Text.Json;
using System.Text.Json.Nodes;
using Sang.Service.TallyIntegration.Models;
using Sang.Service.TallyIntegration.Configurations;

namespace Sang.Service.TallyIntegration.Builders
{
    public class TallyMasterJsonBuilder : ITallyMasterBuilder
    {
        public string Build(TallyMasterRequest request)
        {
            ValidateRequest(request);

            var definition =
                TallyMasterRegistry.Get(request.MasterType);

            var master = new JsonObject
            {
                ["metadata"] = new JsonObject
                {
                    ["type"] = definition.MasterType,
                    ["action"] = definition.JsonAction,
                    ["name"] = request.Master.Name
                },

                ["name"] = request.Master.Name
            };

            // Master code
            if (!string.IsNullOrWhiteSpace(request.Master.Code))
            {
                master[definition.JsonCodeField] =
                    request.Master.Code;
            }

            // Parent
            if (!string.IsNullOrWhiteSpace(request.Master.Parent) &&
                !request.Master.Parent.Trim().Equals(
                    "Primary",
                    StringComparison.OrdinalIgnoreCase))
            {
                master["parent"] = request.Master.Parent.Trim();
            }

            // Extra fields
            foreach (var field in request.Master.ExtraFields)
            {
                ValidateField(field.Key, definition.JsonCodeField);

                master[field.Key] =
                    JsonSerializer.SerializeToNode(field.Value);
            }

            // Static variables
            var staticVariables = new JsonArray
            {
                new JsonObject
                {
                    ["name"] = "svMstImportFormat",
                    ["value"] = "JSONEx"
                }
            };

            // Add company only when provided.
            // Otherwise Tally uses the currently open company.
            if (!string.IsNullOrWhiteSpace(request.CompanyName))
            {
                staticVariables.Add(
                    new JsonObject
                    {
                        ["name"] = "svCurrentCompany",
                        ["value"] = request.CompanyName.Trim()
                    });
            }

            var payload = new JsonObject
            {
                ["static_variables"] = staticVariables,
                ["tallymessage"] = new JsonArray(master)
            };

            return payload.ToJsonString(
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });
        }

        // =========================================================
        // VALIDATE REQUEST
        // =========================================================

        private static void ValidateRequest(
            TallyMasterRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            // CompanyName is optional.

            if (string.IsNullOrWhiteSpace(request.MasterType))
                throw new ArgumentException(
                    "MasterType is required.");

            if (request.Master == null)
                throw new ArgumentException(
                    "Master details are required.");

            if (string.IsNullOrWhiteSpace(request.Master.Name))
                throw new ArgumentException(
                    "Master name is required.");

            request.Master.ExtraFields ??=
                new Dictionary<string, object?>();
        }

        // =========================================================
        // VALIDATE EXTRA FIELDS
        // =========================================================

        private static void ValidateField(
            string fieldName,
            string codeField)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException(
                    "Extra field name is required.");

            var reservedFields = new[]
            {
                "metadata",
                "name",
                "parent",
                codeField
            };

            if (reservedFields.Contains(
                fieldName,
                StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"Extra field '{fieldName}' is reserved.");
            }
        }
    }
}
