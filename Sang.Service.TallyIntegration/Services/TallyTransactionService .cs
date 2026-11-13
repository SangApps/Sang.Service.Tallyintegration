using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Sang.Service.TallyIntegration.Builders;
using Sang.Service.TallyIntegration.Enums;
using Sang.Service.TallyIntegration.Models;

namespace Sang.Service.TallyIntegration.Services
{
    public class TallyTransactionService : ITallyTransactionService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TallyTransactionService> _logger;

        private const string TallyUrl = "http://localhost:9000";

        public TallyTransactionService(
            IHttpClientFactory httpClientFactory,
            ILogger<TallyTransactionService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("TallyPrime");
            _logger = logger;
        }

        // =====================================================
        // POST XML TRANSACTION
        // =====================================================

        public async Task<TallyVoucherResponse> PostTransaction(
            TallyVoucherRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

<<<<<<< Updated upstream
            try
            {
                var tallyUrl = "http://localhost:9000";
=======
            if (string.IsNullOrWhiteSpace(request.VoucherXml))
            {
                return new TallyVoucherResponse
                {
                    Success = false,
                    Code = 400,
                    Result = null,
                    Message = "Voucher XML is required."
                };
            }

            try
            {
                // Validate XML before sending.
                XDocument.Parse(request.VoucherXml);
>>>>>>> Stashed changes

                // VoucherXml temporarily contains the raw JSONEx payload.
                var voucherJson = request.VoucherXml;

<<<<<<< Updated upstream
                if (string.IsNullOrWhiteSpace(voucherJson))
                {
                    throw new ArgumentException(
                        "Tally JSONEx request body is required.",
                        nameof(request));
                }

                using var httpRequest = new HttpRequestMessage(
                    HttpMethod.Post,
                    tallyUrl);

                // JSONEx requires application/json.
                httpRequest.Content = new StringContent(
                    voucherJson,
=======
                _logger.LogInformation(
                    "{VoucherXml}",
                    request.VoucherXml);

                using var content = new StringContent(
                    request.VoucherXml,
>>>>>>> Stashed changes
                    Encoding.UTF8,
                    "application/json");

<<<<<<< Updated upstream
                // TallyPrime JSONEx headers.
                httpRequest.Headers.TryAddWithoutValidation(
                    "version", "1");

                httpRequest.Headers.TryAddWithoutValidation(
                    "tallyrequest", "Import");

                httpRequest.Headers.TryAddWithoutValidation(
                    "type", "Data");

                httpRequest.Headers.TryAddWithoutValidation(
                    "id", "Vouchers");

                httpRequest.Headers.TryAddWithoutValidation(
                    "detailed-response", "Yes");

                _logger.LogInformation(
                    "Sending JSONEx transaction to TallyPrime at {TallyUrl}.",
                    tallyUrl);

                using var response = await _httpClient.SendAsync(
                    httpRequest,
=======
                using var response = await _httpClient.PostAsync(
                    TallyUrl,
                    content,
>>>>>>> Stashed changes
                    cancellationToken);

                var responseBody = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                _logger.LogInformation(
<<<<<<< Updated upstream
                    "TallyPrime returned HTTP {StatusCode}.",
                    (int)response.StatusCode);

                // HTTP success does not necessarily mean that
                // Tally imported the voucher successfully.
=======
                    "Tally XML HTTP Status: {StatusCode}",
                    (int)response.StatusCode);

                _logger.LogInformation(
                    "========== TALLY XML RESPONSE ==========");

                _logger.LogInformation(
                    "{Response}",
                    responseBody);

>>>>>>> Stashed changes
                if (!response.IsSuccessStatusCode)
                {
                    return new TallyVoucherResponse
                    {
                        Success = false,
                        Code = (int)response.StatusCode,
                        Result = null,
                        Message =
                            $"Tally HTTP error: {(int)response.StatusCode} " +
<<<<<<< Updated upstream
                            $"{response.ReasonPhrase}",
                        ResponseXml = responseBody
                    };
                }

                var message = "Tally response received.";
                var success = true;

                if (string.IsNullOrWhiteSpace(responseBody))
                {
                    success = false;
                    message = "Tally returned an empty response.";
                }
                else if (responseBody.Contains(
                    "Unknown Request",
                    StringComparison.OrdinalIgnoreCase))
                {
                    success = false;
                    message =
                        "Tally rejected the request: Unknown Request. " +
                        "Verify the JSONEx payload and HTTP headers.";
                }
                else
                {
                    // Check XML-style Tally responses when present.
                    try
                    {
                        if (responseBody.TrimStart().StartsWith(
                            "<",
                            StringComparison.Ordinal))
                        {
                            var document = XDocument.Parse(responseBody);

                            var errors = document
                                .Descendants()
                                .Where(x =>
                                    x.Name.LocalName.Equals(
                                        "LINEERROR",
                                        StringComparison.OrdinalIgnoreCase))
                                .Select(x => x.Value.Trim())
                                .Where(x => !string.IsNullOrWhiteSpace(x))
                                .ToList();

                            if (errors.Count > 0)
                            {
                                success = false;
                                message = string.Join("; ", errors);
                            }
                            else
                            {
                                var created = document
                                    .Descendants()
                                    .FirstOrDefault(x =>
                                        x.Name.LocalName.Equals(
                                            "CREATED",
                                            StringComparison.OrdinalIgnoreCase))
                                    ?.Value;

                                var altered = document
                                    .Descendants()
                                    .FirstOrDefault(x =>
                                        x.Name.LocalName.Equals(
                                            "ALTERED",
                                            StringComparison.OrdinalIgnoreCase))
                                    ?.Value;

                                var exceptions = document
                                    .Descendants()
                                    .FirstOrDefault(x =>
                                        x.Name.LocalName.Equals(
                                            "EXCEPTIONS",
                                            StringComparison.OrdinalIgnoreCase))
                                    ?.Value;

                                if (int.TryParse(exceptions, out var count)
                                    && count > 0)
                                {
                                    success = false;
                                    message =
                                        $"Tally reported {count} exception(s).";
                                }
                                else if (created != null || altered != null)
                                {
                                    message =
                                        $"Tally response received. " +
                                        $"Created: {created ?? "N/A"}, " +
                                        $"Altered: {altered ?? "N/A"}.";
                                }
                            }
                        }
                    }
                    catch (System.Xml.XmlException)
                    {
                        // JSONEx may return a non-XML response.
                        // Preserve the response for inspection.
                        message =
                            "Tally response received. Review the response " +
                            "body to confirm the import result.";
                    }
                }

                return new TallyVoucherResponse
                {
                    Success = success,
                    Message = message,
                    ResponseXml = responseBody
=======
                            $"{response.ReasonPhrase}"
                    };
                }

                return TallyImportResponseParser.Parse(
                    responseBody,
                    TallyFormat.Xml,
                    $"Voucher TransId: {request.TransId}");
            }
            catch (System.Xml.XmlException ex)
            {
                _logger.LogError(
                    ex,
                    "VoucherXml is invalid XML.");

                return new TallyVoucherResponse
                {
                    Success = false,
                    Code = 400,
                    Result = null,
                    Message = $"Invalid voucher XML: {ex.Message}"
>>>>>>> Stashed changes
                };
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "Tally XML transaction posting was cancelled.");

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error posting XML transaction to Tally.");

<<<<<<< Updated upstream
                throw;
=======
                return new TallyVoucherResponse
                {
                    Success = false,
                    Code = 500,
                    Result = null,
                    Message = "An error occurred while posting the XML transaction to Tally."
                };
            }
        }

        // =====================================================
        // POST JSON TRANSACTION
        // =====================================================

        public async Task<TallyVoucherResponse> PostJsonTransaction(
            TallyVoucherJsonRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.VoucherJson))
            {
                return new TallyVoucherResponse
                {
                    Success = false,
                    Code = 400,
                    Result = null,
                    Message = "Voucher JSON is required."
                };
            }

            try
            {
                // Validate JSON before sending.
                using var jsonDocument = JsonDocument.Parse(
                    request.VoucherJson);

                using var httpRequest = new HttpRequestMessage(
                    HttpMethod.Post,
                    TallyUrl);

                // Required TallyPrime JSON headers.
                httpRequest.Headers.TryAddWithoutValidation(
                    "version", "1");

                httpRequest.Headers.TryAddWithoutValidation(
                    "tallyrequest", "Import");

                httpRequest.Headers.TryAddWithoutValidation(
                    "type", "Data");

                httpRequest.Headers.TryAddWithoutValidation(
                    "id", "Vouchers");

                httpRequest.Headers.TryAddWithoutValidation(
                    "detailed-response", "Yes");

                httpRequest.Content = new StringContent(
                    request.VoucherJson,
                    Encoding.UTF8,
                    "application/json");

                _logger.LogInformation(
                    "========== TALLY JSON REQUEST ==========");

                _logger.LogInformation(
                    "{VoucherJson}",
                    request.VoucherJson);

                using var response = await _httpClient.SendAsync(
                    httpRequest,
                    cancellationToken);

                var responseBody = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                _logger.LogInformation(
                    "Tally JSON HTTP Status: {StatusCode}",
                    (int)response.StatusCode);

                _logger.LogInformation(
                    "========== TALLY JSON RESPONSE ==========");

                _logger.LogInformation(
                    "{Response}",
                    responseBody);

                if (!response.IsSuccessStatusCode)
                {
                    return new TallyVoucherResponse
                    {
                        Success = false,
                        Code = (int)response.StatusCode,
                        Result = null,
                        Message =
                            $"Tally HTTP error: {(int)response.StatusCode} " +
                            $"{response.ReasonPhrase}"
                    };
                }

                return TallyImportResponseParser.Parse(
                    responseBody,
                    TallyFormat.Json,
                    $"Voucher TransId: {request.TransId}");
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "VoucherJson is invalid JSON.");

                return new TallyVoucherResponse
                {
                    Success = false,
                    Code = 400,
                    Result = null,
                    Message = $"Invalid voucher JSON: {ex.Message}"
                };
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "Tally JSON transaction posting was cancelled.");

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error posting JSON transaction to Tally.");

                return new TallyVoucherResponse
                {
                    Success = false,
                    Code = 500,
                    Result = null,
                    Message = "An error occurred while posting the JSON transaction to Tally."
                };
>>>>>>> Stashed changes
            }
        }
    }
}