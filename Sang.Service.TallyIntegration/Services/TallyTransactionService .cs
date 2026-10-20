using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Sang.Service.TallyIntegration.Models;

namespace Sang.Service.TallyIntegration.Services
{
    public class TallyTransactionService : ITallyTransactionService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TallyTransactionService> _logger;

        public TallyTransactionService(
            IHttpClientFactory httpClientFactory,
            ILogger<TallyTransactionService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("TallyPrime");
            _logger = logger;
        }

        public async Task<TallyVoucherResponse> PostTransaction(
            TallyVoucherRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            const string tallyUrl = "http://localhost:9000";

            var voucherXml = request.VoucherXml;

            if (string.IsNullOrWhiteSpace(voucherXml))
            {
                return new TallyVoucherResponse
                {
                    Success = false,
                    Message = "Voucher XML is required.",
                    ResponseXml = string.Empty
                };
            }

            try
            {
                // Validate XML
                XDocument.Parse(voucherXml);

                _logger.LogInformation(
                    "========== TALLY XML REQUEST ==========");

                _logger.LogInformation(
                    "{VoucherXml}",
                    voucherXml);

                // IMPORTANT:
                // This is XML, NOT JSONEx.
                using var content = new StringContent(
                    voucherXml,
                    Encoding.UTF8,
                    "text/xml");

                using var response = await _httpClient.PostAsync(
                    tallyUrl,
                    content,
                    cancellationToken);

                var responseBody =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                _logger.LogInformation(
                    "Tally HTTP Status: {StatusCode}",
                    (int)response.StatusCode);

                _logger.LogInformation(
                    "========== TALLY RESPONSE ==========");

                _logger.LogInformation(
                    "{Response}",
                    responseBody);

                if (!response.IsSuccessStatusCode)
                {
                    return new TallyVoucherResponse
                    {
                        Success = false,
                        Message =
                            $"Tally HTTP error: " +
                            $"{(int)response.StatusCode} " +
                            $"{response.ReasonPhrase}",
                        ResponseXml = responseBody
                    };
                }

                return ParseTallyResponse(responseBody);
            }
            catch (System.Xml.XmlException ex)
            {
                _logger.LogError(
                    ex,
                    "VoucherXml is invalid XML.");

                return new TallyVoucherResponse
                {
                    Success = false,
                    Message =
                        $"Invalid voucher XML: {ex.Message}",
                    ResponseXml = voucherXml
                };
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "Tally transaction posting was cancelled.");

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error posting transaction to Tally.");

                throw;
            }
        }

        private TallyVoucherResponse ParseTallyResponse(
            string responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return new TallyVoucherResponse
                {
                    Success = false,
                    Message = "Tally returned an empty response.",
                    ResponseXml = responseBody
                };
            }

            try
            {
                var document = XDocument.Parse(responseBody);

                var lineErrors = document
                    .Descendants()
                    .Where(x =>
                        x.Name.LocalName.Equals(
                            "LINEERROR",
                            StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.Value.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                if (lineErrors.Any())
                {
                    return new TallyVoucherResponse
                    {
                        Success = false,
                        Message = string.Join(
                            "; ",
                            lineErrors),
                        ResponseXml = responseBody
                    };
                }

                var created = GetValue(
                    document,
                    "CREATED");

                var altered = GetValue(
                    document,
                    "ALTERED");

                var exceptions = GetValue(
                    document,
                    "EXCEPTIONS");

                var errors = GetValue(
                    document,
                    "ERRORS");

                var createdCount = ParseInt(created);
                var alteredCount = ParseInt(altered);
                var exceptionCount = ParseInt(exceptions);
                var errorCount = ParseInt(errors);

                var success =
                    exceptionCount == 0 &&
                    errorCount == 0 &&
                    (createdCount > 0 ||
                     alteredCount > 0);

                var message = success
                    ? $"Tally transaction posted successfully. " +
                      $"Created: {createdCount}, " +
                      $"Altered: {alteredCount}."
                    : $"Tally transaction was not posted. " +
                      $"Created: {createdCount}, " +
                      $"Altered: {alteredCount}, " +
                      $"Errors: {errorCount}, " +
                      $"Exceptions: {exceptionCount}.";

                return new TallyVoucherResponse
                {
                    Success = success,
                    Message = message,
                    ResponseXml = responseBody
                };
            }
            catch (System.Xml.XmlException)
            {
                return new TallyVoucherResponse
                {
                    Success = false,
                    Message =
                        "Tally returned a non-XML response.",
                    ResponseXml = responseBody
                };
            }
        }

        private static string GetValue(
            XDocument document,
            string elementName)
        {
            return document
                .Descendants()
                .FirstOrDefault(x =>
                    x.Name.LocalName.Equals(
                        elementName,
                        StringComparison.OrdinalIgnoreCase))
                ?.Value
                ?? "0";
        }

        private static int ParseInt(string value)
        {
            return int.TryParse(
                value,
                out var result)
                ? result
                : 0;
        }

        public async Task<TallyVoucherResponse> PostJsonTransaction(
    TallyVoucherJsonRequest request,
    CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            const string tallyUrl = "http://localhost:9000";

            if (string.IsNullOrWhiteSpace(request.VoucherJson))
            {
                return new TallyVoucherResponse
                {
                    Success = false,
                    Message = "Voucher JSON is required.",
                    ResponseXml = string.Empty
                };
            }

            try
            {
                // Validate the JSON string.
                using var jsonDocument = JsonDocument.Parse(request.VoucherJson);

                using var httpRequest = new HttpRequestMessage(
                    HttpMethod.Post,
                    tallyUrl);

                httpRequest.Headers.TryAddWithoutValidation("version", "1");
                httpRequest.Headers.TryAddWithoutValidation("tallyrequest", "Import");
                httpRequest.Headers.TryAddWithoutValidation("type", "Data");
                httpRequest.Headers.TryAddWithoutValidation("id", "Vouchers");
                httpRequest.Headers.TryAddWithoutValidation("detailed-response", "Yes");

                httpRequest.Content = new StringContent(
                    request.VoucherJson,
                    Encoding.UTF8,
                    "application/json");

                using var response = await _httpClient.SendAsync(
                    httpRequest,
                    cancellationToken);

                var responseBody = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return new TallyVoucherResponse
                    {
                        Success = false,
                        Message = $"Tally HTTP error: {(int)response.StatusCode} " +
                                  $"{response.ReasonPhrase}",
                        ResponseXml = responseBody
                    };
                }

                return new TallyVoucherResponse
                {
                    Success = true,
                    Message = "Request sent to Tally. Verify the voucher import result.",
                    ResponseXml = responseBody
                };
            }
            catch (JsonException ex)
            {
                return new TallyVoucherResponse
                {
                    Success = false,
                    Message = $"Invalid voucher JSON: {ex.Message}",
                    ResponseXml = request.VoucherJson
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting JSON transaction to Tally.");
                throw;
            }
        }
    }
}