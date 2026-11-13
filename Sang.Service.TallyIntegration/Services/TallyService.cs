
using Microsoft.Extensions.Logging;
using Sang.Service.TallyIntegration.Enums;
using Sang.Service.TallyIntegration.Models;
using Sang.Service.TallyIntegration.Configurations;
using System.Text;
using System.Xml.Linq;
using Sang.Service.TallyIntegration.Builders;

namespace Sang.Service.TallyIntegration.Services
{
    public class TallyService : ITallyService
    {
        private readonly HttpClient _httpClient;
        private readonly ITallyTransactionService _tallyTransactionService;
        private readonly TallyMasterJsonBuilder _jsonBuilder;
        private readonly TallyMasterXmlBuilder _xmlBuilder;
        private readonly ILogger<TallyService> _logger;

        private const string TallyUrl = "http://localhost:9000";

        public TallyService(
            IHttpClientFactory httpClientFactory,
            ITallyTransactionService tallyTransactionService,
            TallyMasterJsonBuilder jsonBuilder,
            TallyMasterXmlBuilder xmlBuilder,
            ILogger<TallyService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("TallyPrime");
            _tallyTransactionService = tallyTransactionService;
            _jsonBuilder = jsonBuilder;
            _xmlBuilder = xmlBuilder;
            _logger = logger;
        }

        // =========================================================
        // GET LEDGERS
        // =========================================================

        public async Task<List<TallyLedgerDto>> GetLedgers(
            CancellationToken cancellationToken = default)
        {
            try
            {
                var masters = await GetMasters(
                    "List of Ledgers",
                    "LEDGER",
                    cancellationToken);

                return masters.Select(x => new TallyLedgerDto
                {
                    Name = x.Name,
                    Parent = x.Parent
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Tally ledgers.");
                throw;
            }
        }

        // =========================================================
        // GET PRODUCTS / STOCK ITEMS
        // =========================================================

        public Task<List<TallyMasterDto>> GetProducts(
            CancellationToken cancellationToken = default)
        {
            return GetMasters(
                "List of Stock Items",
                "STOCKITEM",
                cancellationToken);
        }

        // =========================================================
        // GET WAREHOUSES / GODOWNS
        // =========================================================

        public Task<List<TallyMasterDto>> GetWarehouses(
            CancellationToken cancellationToken = default)
        {
            return GetMasters(
                "List of Godowns",
                "GODOWN",
                cancellationToken);
        }

        // =========================================================
        // GET UNITS
        // =========================================================

        public Task<List<TallyMasterDto>> GetUnits(
            CancellationToken cancellationToken = default)
        {
            return GetMasters(
                "List of Units",
                "UNIT",
                cancellationToken);
        }

        // =========================================================
        // GET COMPANY DETAILS
        // =========================================================

        public async Task<string> GetCompanyDetails(
            CancellationToken cancellationToken = default)
        {
            try
            {
                var xml = BuildExportRequest("DATA", "Company");

                return await SendXml(xml, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving Tally company details.");

                throw;
            }
        }

        // =========================================================
        // GET VOUCHERS
        // =========================================================

        public async Task<string> GetVouchers(
            CancellationToken cancellationToken = default)
        {
            try
            {
                var xml = BuildExportRequest("COLLECTION", "Day Book");

                return await SendXml(xml, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving Tally vouchers.");

                throw;
            }
        }

        // =========================================================
        // CREATE MASTER - XML / JSON
        // =========================================================

        public async Task<TallyVoucherResponse> CreateMaster(
            TallyMasterRequest request,
            TallyFormat format,
            CancellationToken cancellationToken = default)
        {
            ValidateMasterRequest(request);

            var payload = format switch
            {
                TallyFormat.Xml => _xmlBuilder.Build(request),

                TallyFormat.Json => _jsonBuilder.Build(request),

                _ => throw new ArgumentOutOfRangeException(
                    nameof(format),
                    format,
                    "Unsupported Tally format. Use Xml or Json.")
            };

            var response = format switch
            {
                TallyFormat.Xml => await SendXml(
                    payload,
                    cancellationToken),

                TallyFormat.Json => await SendJson(
                    payload,
                    "All Masters",
                    cancellationToken),

                _ => throw new ArgumentOutOfRangeException(
                    nameof(format))
            };

            // Use the shared parser for both XML and JSON.
            return TallyImportResponseParser.Parse(
                response,
                format,
                $"{request.MasterType} '{request.Master.Name}'");
        }

        // =========================================================
        // POST XML TRANSACTION
        // =========================================================

        public Task<TallyVoucherResponse> PostXmlTransaction(
            TallyVoucherRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            return _tallyTransactionService.PostTransaction(
                request,
                cancellationToken);
        }

        // =========================================================
        // POST JSON TRANSACTION
        // =========================================================

        public Task<TallyVoucherResponse> PostJsonTransaction(
            TallyVoucherJsonRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            return _tallyTransactionService.PostJsonTransaction(
                request,
                cancellationToken);
        }

        // =========================================================
        // PRIVATE HELPER: GET MASTERS
        // =========================================================

        private async Task<List<TallyMasterDto>> GetMasters(
            string collectionName,
            string elementName,
            CancellationToken cancellationToken)
        {
            try
            {
                var xml = BuildExportRequest(
                    "COLLECTION",
                    collectionName);

                var response = await SendXml(
                    xml,
                    cancellationToken);

                var document = XDocument.Parse(response);

                return document
                    .Descendants()
                    .Where(x => x.Name.LocalName == elementName)
                    .Select(x => new TallyMasterDto
                    {
                        Name = GetValue(x, "NAME") is { Length: > 0 } name
                            ? name
                            : (string?)x.Attribute("NAME") ?? string.Empty,

                        Parent = GetValue(x, "PARENT")
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving Tally master collection {CollectionName}.",
                    collectionName);

                throw;
            }
        }

        // =========================================================
        // PRIVATE HELPER: BUILD EXPORT XML
        // =========================================================

        private string BuildExportRequest(string type, string id)
        {
            return new XDocument(
                new XElement("ENVELOPE",
                    new XElement("HEADER",
                        new XElement("VERSION", "1"),
                        new XElement("TALLYREQUEST", "EXPORT"),
                        new XElement("TYPE", type),
                        new XElement("ID", id)
                    ),
                    new XElement("BODY",
                        new XElement("DESC",
                            new XElement("STATICVARIABLES",
                                new XElement(
                                    "SVEXPORTFORMAT",
                                    "$$SysName:XML")
                            )
                        )
                    )
                )
            ).ToString();
        }

        // =========================================================
        // PRIVATE HELPER: SEND XML TO TALLY
        // =========================================================

        private async Task<string> SendXml(
            string xml,
            CancellationToken cancellationToken)
        {
            using var content = new StringContent(
                xml,
                Encoding.UTF8,
                "text/xml");

            using var response = await _httpClient.PostAsync(
                TallyUrl,
                content,
                cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(
                cancellationToken);

            response.EnsureSuccessStatusCode();

            return responseBody;
        }

        // =========================================================
        // PRIVATE HELPER: SEND JSON TO TALLY
        // =========================================================

        private async Task<string> SendJson(
            string json,
            string requestId,
            CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                TallyUrl);

            // Required TallyPrime JSON integration headers.
            request.Headers.TryAddWithoutValidation("version", "1");
            request.Headers.TryAddWithoutValidation("tallyrequest", "Import");
            request.Headers.TryAddWithoutValidation("type", "Data");
            request.Headers.TryAddWithoutValidation("id", requestId);

            request.Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(
                cancellationToken);

            _logger.LogInformation(
                "Tally JSON request completed. RequestId: {RequestId}, StatusCode: {StatusCode}",
                requestId,
                response.StatusCode);

            _logger.LogInformation(
                "Tally JSON response: {Response}",
                responseBody);

            response.EnsureSuccessStatusCode();

            return responseBody;
        }

        // =========================================================
        // PRIVATE HELPER: READ XML ELEMENT
        // =========================================================

        private static string GetValue(
            XElement element,
            string name)
        {
            return element
                .Elements()
                .FirstOrDefault(x => x.Name.LocalName == name)
                ?.Value ?? string.Empty;
        }

        // =========================================================
        // PRIVATE HELPER: VALIDATE MASTER REQUEST
        // =========================================================

        private static void ValidateMasterRequest(
            TallyMasterRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            // CompanyName is optional.
            // If empty, Tally uses the currently open company.

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

            // Validate supported master type.
            TallyMasterRegistry.Get(request.MasterType);
        }
    }
}
