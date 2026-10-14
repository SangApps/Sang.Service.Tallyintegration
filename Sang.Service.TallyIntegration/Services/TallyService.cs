using Microsoft.Extensions.Logging;
using Sang.Service.TallyIntegration.Models;
using System.Security;
using System.Text;
using System.Xml.Linq;

namespace Sang.Service.TallyIntegration.Services
{
    public class TallyService : ITallyService
    {
        private readonly HttpClient _httpClient;
        private readonly ITallyTransactionService _tallyTransactionService;
        private readonly ILogger<TallyService> _logger;

        public TallyService(
                     IHttpClientFactory httpClientFactory,
                     ITallyTransactionService tallyTransactionService,
                     ILogger<TallyService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("TallyPrime");
            _tallyTransactionService = tallyTransactionService;
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
                    Parent = x.Parent,
                    MasterId = x.MasterId,
                    ClosingBalance = x.ClosingBalance
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving Tally ledgers.");

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
                var xml = BuildExportRequest(
                    "DATA",
                    "Company");

                return await SendXml(
                    xml,
                    cancellationToken);
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
                var xml = BuildExportRequest(
                    "COLLECTION",
                    "Day Book");

                return await SendXml(
                    xml,
                    cancellationToken);
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
        // CREATE LEDGER
        // =========================================================

        public async Task<string> CreateLedger(
            string name,
            string parent,
            CancellationToken cancellationToken = default)
        {
            ValidateRequired(name, nameof(name));
            ValidateRequired(parent, nameof(parent));

            try
            {
                var escapedName = EscapeXml(name);
                var escapedParent = EscapeXml(parent);

                var xml = $"""
                    <ENVELOPE>
                        <HEADER>
                            <VERSION>1</VERSION>
                            <TALLYREQUEST>IMPORT</TALLYREQUEST>
                            <TYPE>DATA</TYPE>
                            <ID>All Masters</ID>
                        </HEADER>
                        <BODY>
                            <IMPORTDATA>
                                <REQUESTDESC>
                                    <REPORTNAME>All Masters</REPORTNAME>
                                </REQUESTDESC>
                                <REQUESTDATA>
                                    <TALLYMESSAGE xmlns:UDF="TallyUDF">
                                        <LEDGER NAME="{escapedName}" ACTION="Create">
                                            <NAME>{escapedName}</NAME>
                                            <PARENT>{escapedParent}</PARENT>
                                        </LEDGER>
                                    </TALLYMESSAGE>
                                </REQUESTDATA>
                            </IMPORTDATA>
                        </BODY>
                    </ENVELOPE>
                    """;

                return await SendXml(
                    xml,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating Tally ledger {LedgerName}.",
                    name);

                throw;
            }
        }

        // =========================================================
        // CREATE PRODUCT / STOCK ITEM
        // =========================================================

        public async Task<string> CreateProduct(
            string name,
            string parent,
            string baseUnits,
            CancellationToken cancellationToken = default)
        {
            ValidateRequired(name, nameof(name));
            ValidateRequired(parent, nameof(parent));
            ValidateRequired(baseUnits, nameof(baseUnits));

            try
            {
                var escapedName = EscapeXml(name);
                var escapedParent = EscapeXml(parent);
                var escapedUnits = EscapeXml(baseUnits);

                var xml = $"""
                    <ENVELOPE>
                        <HEADER>
                            <VERSION>1</VERSION>
                            <TALLYREQUEST>IMPORT</TALLYREQUEST>
                            <TYPE>DATA</TYPE>
                            <ID>All Masters</ID>
                        </HEADER>
                        <BODY>
                            <IMPORTDATA>
                                <REQUESTDESC>
                                    <REPORTNAME>All Masters</REPORTNAME>
                                </REQUESTDESC>
                                <REQUESTDATA>
                                    <TALLYMESSAGE xmlns:UDF="TallyUDF">
                                        <STOCKITEM NAME="{escapedName}" ACTION="Create">
                                            <NAME>{escapedName}</NAME>
                                            <PARENT>{escapedParent}</PARENT>
                                            <BASEUNITS>{escapedUnits}</BASEUNITS>
                                        </STOCKITEM>
                                    </TALLYMESSAGE>
                                </REQUESTDATA>
                            </IMPORTDATA>
                        </BODY>
                    </ENVELOPE>
                    """;

                return await SendXml(
                    xml,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating Tally stock item {ProductName}.",
                    name);

                throw;
            }
        }

        // =========================================================
        // POST VANSALES TRANSACTION TO TALLY
        // =========================================================

        public Task<TallyVoucherResponse> PostTransaction(
            TallyVoucherRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return _tallyTransactionService.PostTransaction(
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
                            : (string?)x.Attribute("NAME")
                                ?? string.Empty,

                        Parent = GetValue(x, "PARENT"),
                        MasterId = GetValue(x, "MASTERID"),
                        BaseUnits = GetValue(x, "BASEUNITS"),
                        OpeningBalance = GetValue(x, "OPENINGBALANCE"),
                        ClosingBalance = GetValue(x, "CLOSINGBALANCE")
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
            var companyName = "TallySettings:CompanyName" ?? string.Empty;

            var companyXml = string.IsNullOrWhiteSpace(companyName)
                ? string.Empty
                : $"""
            <SVCURRENTCOMPANY>
                {EscapeXml(companyName)}
            </SVCURRENTCOMPANY>
            """;

            return $"""
        <ENVELOPE>
            <HEADER>
                <VERSION>1</VERSION>
                <TALLYREQUEST>EXPORT</TALLYREQUEST>
                <TYPE>{EscapeXml(type)}</TYPE>
                <ID>{EscapeXml(id)}</ID>
            </HEADER>
            <BODY>
                <DESC>
                    <STATICVARIABLES>
                        <SVEXPORTFORMAT>$$SysName:XML</SVEXPORTFORMAT>
                        {companyXml}
                    </STATICVARIABLES>
                </DESC>
            </BODY>
        </ENVELOPE>
        """;
        }

        // =========================================================
        // PRIVATE HELPER: SEND XML TO TALLY
        // =========================================================

        private async Task<string> SendXml(string xml,CancellationToken cancellationToken)
        {
            var tallyUrl = "http://localhost:9000";

            using var content = new StringContent(
                xml,
                Encoding.UTF8,
                "text/xml");

            using var response = await _httpClient.PostAsync(
                tallyUrl,
                content,
                cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(
                cancellationToken);

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
        // PRIVATE HELPER: XML ESCAPING
        // =========================================================

        private static string EscapeXml(string value)
        {
            return SecurityElement.Escape(value)
                ?? string.Empty;
        }

        // =========================================================
        // PRIVATE HELPER: VALIDATION
        // =========================================================

        private static void ValidateRequired(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    $"{parameterName} is required.",
                    parameterName);
            }
        }
    }
}