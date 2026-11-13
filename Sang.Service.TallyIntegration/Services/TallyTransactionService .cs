using System.Text;
using System.Text.Json;
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

                _logger.LogInformation(
                    "========== TALLY XML REQUEST ==========");

                _logger.LogInformation(
                    "{VoucherXml}",
                    request.VoucherXml);

                using var content = new StringContent(
                    request.VoucherXml,
                    Encoding.UTF8,
                    "text/xml");

                using var response = await _httpClient.PostAsync(
                    TallyUrl,
                    content,
                    cancellationToken);

                var responseBody = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                _logger.LogInformation(
                    "Tally XML HTTP Status: {StatusCode}",
                    (int)response.StatusCode);

                _logger.LogInformation(
                    "========== TALLY XML RESPONSE ==========");

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
            }
        }
    }
}