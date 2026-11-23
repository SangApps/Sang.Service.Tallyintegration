using Microsoft.Extensions.Logging;
using Sang.Service.TallyIntegration.Builders;
using Sang.Service.TallyIntegration.Models;

namespace Sang.Service.TallyIntegration.Services
{
    public class TallyVoucherService : ITallyVoucherService
    {
        private readonly TallyVoucherJsonBuilder _builder;
        private readonly ITallyTransactionService _transactionService;
        private readonly ILogger<TallyVoucherService> _logger;

        public TallyVoucherService(
            TallyVoucherJsonBuilder builder,
            ITallyTransactionService transactionService,
            ILogger<TallyVoucherService> logger)
        {
            _builder = builder;
            _transactionService = transactionService;
            _logger = logger;
        }

        public Task<TallyVoucherApiResponse> CreateSales(
            SalesVoucherRequest request, CancellationToken cancellationToken = default)
            => Post(request, "Sales", _builder.BuildSales, cancellationToken);

        public Task<TallyVoucherApiResponse> CreateReceipt(
            ReceiptVoucherRequest request, CancellationToken cancellationToken = default)
            => Post(request, "Receipt", _builder.BuildReceipt, cancellationToken);

        public Task<TallyVoucherApiResponse> CreateSalesOrder(
            SalesOrderVoucherRequest request, CancellationToken cancellationToken = default)
            => Post(request, "Sales Order", _builder.BuildSalesOrder, cancellationToken);

        public Task<TallyVoucherApiResponse> CreateSalesReturn(
            SalesReturnVoucherRequest request, CancellationToken cancellationToken = default)
            => Post(request, "Credit Note", _builder.BuildSalesReturn, cancellationToken);

        private async Task<TallyVoucherApiResponse> Post<T>(
            T request, string voucherType, Func<T, string> build, CancellationToken ct)
            where T : TallyVoucherRequestBase
        {
            ArgumentNullException.ThrowIfNull(request);

            string json;
            try
            {
                json = build(request);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid voucher request. TransId: {TransId}", request.TransId);
                return Build(request, voucherType, false, ex.Message, string.Empty);
            }

            var inner = await _transactionService.PostJsonTransaction(
                new TallyVoucherJsonRequest
                {
                    TransId = request.TransId,
                    DocType = request.DocType,
                    IsForIssue = request.IsForIssue,
                    VoucherJson = json
                },
                ct);

            string innerMessage = inner.Success
                ? $"{voucherType} '{request.VoucherNumber}' processed successfully. " +
                  $"Created: {inner.Result?.Created ?? 0}, " +
                  $"Altered: {inner.Result?.Altered ?? 0}, " +
                  $"Deleted: {inner.Result?.Deleted ?? 0}."
                : inner.Message;

            return Build(request, voucherType, inner.Success, innerMessage, inner.RawResponse);
        }

        private static TallyVoucherApiResponse Build(
            TallyVoucherRequestBase request, string voucherType,
            bool success, string innerMessage, string raw)
        {
            return new TallyVoucherApiResponse
            {
                Success = success,
                Message = success
                    ? "Tally voucher created successfully."
                    : "Tally voucher could not be created.",
                VoucherType = voucherType,
                VoucherNumber = request.VoucherNumber,
                Format = "Json",
                Response = new TallyVoucherInnerResponse
                {
                    Success = success,
                    Message = innerMessage,
                    ResponseXml = raw
                }
            };
        }
    }
}
