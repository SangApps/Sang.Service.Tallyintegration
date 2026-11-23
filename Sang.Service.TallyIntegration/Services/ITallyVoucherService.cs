using Sang.Service.TallyIntegration.Models;

namespace Sang.Service.TallyIntegration.Services
{
    public interface ITallyVoucherService
    {
        Task<TallyVoucherApiResponse> CreateSales(
            SalesVoucherRequest request, CancellationToken cancellationToken = default);

        Task<TallyVoucherApiResponse> CreateReceipt(
            ReceiptVoucherRequest request, CancellationToken cancellationToken = default);

        Task<TallyVoucherApiResponse> CreateSalesOrder(
            SalesOrderVoucherRequest request, CancellationToken cancellationToken = default);

        Task<TallyVoucherApiResponse> CreateSalesReturn(
            SalesReturnVoucherRequest request, CancellationToken cancellationToken = default);
    }
}