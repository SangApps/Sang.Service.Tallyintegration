using System.Text;
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

            try
            {
                var tallyUrl = "http://localhost:9000";

                // VoucherXml temporarily contains the raw JSONEx payload.
                var voucherJson = request.VoucherXml;

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
                    Encoding.UTF8,
                    "application/json");

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
                    cancellationToken);

                var responseBody =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                _logger.LogInformation(
                    "TallyPrime returned HTTP {StatusCode}.",
                    (int)response.StatusCode);

                // HTTP success does not necessarily mean that
                // Tally imported the voucher successfully.
                if (!response.IsSuccessStatusCode)
                {
                    return new TallyVoucherResponse
                    {
                        Success = false,
                        Message =
                            $"Tally HTTP error: {(int)response.StatusCode} " +
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
    }
}