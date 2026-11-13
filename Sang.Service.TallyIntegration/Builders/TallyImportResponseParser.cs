using System.Text.Json.Nodes;
using System.Xml.Linq;
using Sang.Service.TallyIntegration.Enums;
using Sang.Service.TallyIntegration.Models;

namespace Sang.Service.TallyIntegration.Builders
{
    public static class TallyImportResponseParser
    {
        public static TallyVoucherResponse Parse(
            string body,
            TallyFormat format,
            string subject)
        {
            if (string.IsNullOrWhiteSpace(body))
                return Fail($"{subject}: Tally returned an empty response.");

            try
            {
                var (
                    status,
                    created,
                    altered,
                    deleted,
                    ignored,
                    errors,
                    exceptions,
                    lineErrors,
                    tallyText
                ) = format == TallyFormat.Json
                    ? ReadJson(body)
                    : ReadXml(body);

                if (lineErrors.Count > 0)
                {
                    return Fail(
                        $"{subject}: {string.Join("; ", lineErrors)}");
                }

                if (created is null && altered is null && deleted is null)
                {
                    var detail = string.IsNullOrWhiteSpace(tallyText)
                        ? string.Empty
                        : $" Tally said: {tallyText}";

                    return Fail(
                        $"{subject}: Tally did not return an import result.{detail}");
                }

                var success =
                    status == "1" &&
                    (errors ?? 0) == 0 &&
                    (exceptions ?? 0) == 0 &&
                    ((created ?? 0) > 0 ||
                     (altered ?? 0) > 0 ||
                     (deleted ?? 0) > 0);

                if (!success)
                {
                    return new TallyVoucherResponse
                    {
                        Success = false,
                        Code = 400,
                        Result = null,
                        Message =
                            $"{subject}: operation failed. " +
                            $"Errors: {errors ?? 0}, " +
                            $"Exceptions: {exceptions ?? 0}."
                    };
                }

                // Include only the action actually performed.
                var result = new TallyImportResult();
                string message;

                if ((created ?? 0) > 0)
                {
                    result.Created = created;
                    message = $"{subject} created successfully.";
                }
                else if ((altered ?? 0) > 0)
                {
                    result.Altered = altered;
                    message = $"{subject} updated successfully.";
                }
                else
                {
                    result.Deleted = deleted;
                    message = $"{subject} deleted successfully.";
                }

                return new TallyVoucherResponse
                {
                    Success = true,
                    Code = 200,
                    Result = result,
                    Message = message
                };
            }
            catch (Exception ex) when (
                ex is System.Xml.XmlException or
                System.Text.Json.JsonException)
            {
                return Fail(
                    $"{subject}: Tally returned a response in an unexpected format.");
            }
        }

        // =====================================================
        // FAILURE RESPONSE
        // =====================================================

        private static TallyVoucherResponse Fail(string message)
        {
            return new TallyVoucherResponse
            {
                Success = false,
                Code = 400,
                Result = null,
                Message = message
            };
        }

        // =====================================================
        // READ XML RESPONSE
        // =====================================================

        private static (
            string,
            int?,
            int?,
            int?,
            int?,
            int?,
            int?,
            List<string>,
            string
        ) ReadXml(string body)
        {
            var doc = XDocument.Parse(body);

            string? Value(string name)
            {
                return doc.Descendants()
                    .FirstOrDefault(x =>
                        x.Name.LocalName.Equals(
                            name,
                            StringComparison.OrdinalIgnoreCase))
                    ?.Value.Trim();
            }

            int? Num(string name)
            {
                var value = Value(name);

                return int.TryParse(value, out var number)
                    ? number
                    : null;
            }

            var lineErrors = doc.Descendants()
                .Where(x => x.Name.LocalName.Equals(
                    "LINEERROR",
                    StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Value.Trim())
                .Where(x => x.Length > 0)
                .ToList();

            var text = doc.Descendants()
                .FirstOrDefault(x =>
                    x.Name.LocalName.Equals(
                        "DATA",
                        StringComparison.OrdinalIgnoreCase)
                    && !x.HasElements)
                ?.Value.Trim() ?? string.Empty;

            return (
                Value("STATUS") ?? "0",
                Num("CREATED"),
                Num("ALTERED"),
                Num("DELETED"),
                Num("IGNORED"),
                Num("ERRORS"),
                Num("EXCEPTIONS"),
                lineErrors,
                text
            );
        }

        // =====================================================
        // READ JSON RESPONSE
        // =====================================================

        private static (
            string,
            int?,
            int?,
            int?,
            int?,
            int?,
            int?,
            List<string>,
            string
        ) ReadJson(string body)
        {
            var found = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

            var lineErrors = new List<string>();

            Walk(JsonNode.Parse(body), found, lineErrors);

            int? Num(string name)
            {
                return found.TryGetValue(name, out var value) &&
                       int.TryParse(value, out var number)
                    ? number
                    : null;
            }

            found.TryGetValue("status", out var status);
            found.TryGetValue("message", out var text);

            return (
                status ?? "0",
                Num("created"),
                Num("altered"),
                Num("deleted"),
                Num("ignored"),
                Num("errors"),
                Num("exceptions"),
                lineErrors,
                text ?? string.Empty
            );
        }

        // =====================================================
        // READ JSON NODES RECURSIVELY
        // =====================================================

        private static void Walk(
            JsonNode? node,
            Dictionary<string, string> found,
            List<string> lineErrors)
        {
            switch (node)
            {
                case JsonObject obj:

                    foreach (var (key, value) in obj)
                    {
                        if (value is JsonValue jsonValue)
                        {
                            var text = jsonValue.ToString();

                            if (key.Equals(
                                "lineerror",
                                StringComparison.OrdinalIgnoreCase))
                            {
                                if (!string.IsNullOrWhiteSpace(text))
                                    lineErrors.Add(text);
                            }
                            else
                            {
                                found.TryAdd(key, text);
                            }
                        }
                        else
                        {
                            Walk(value, found, lineErrors);
                        }
                    }

                    break;

                case JsonArray array:

                    foreach (var item in array)
                    {
                        Walk(item, found, lineErrors);
                    }

                    break;
            }
        }
    }
}