using Sang.Service.TallyIntegration.Models;
using Sang.Service.TallyIntegration.Enums;

namespace Sang.Service.TallyIntegration.Services
{
    public interface ITallyService
    {
        // Get Masters
        Task<List<TallyLedgerDto>> GetLedgers(
            CancellationToken cancellationToken = default);

        Task<List<TallyMasterDto>> GetProducts(
            CancellationToken cancellationToken = default);

        Task<List<TallyMasterDto>> GetWarehouses(
            CancellationToken cancellationToken = default);

        Task<List<TallyMasterDto>> GetUnits(
            CancellationToken cancellationToken = default);

        // Get Company Details
        Task<string> GetCompanyDetails(
            CancellationToken cancellationToken = default);

        // Get Vouchers
        Task<string> GetVouchers(
            CancellationToken cancellationToken = default);

        // Common Master Creation (JSON / XML)
        Task<TallyVoucherResponse> CreateMaster(
            TallyMasterRequest request,
            TallyFormat format,
            CancellationToken cancellationToken = default);

        // Voucher Transactions
        Task<TallyVoucherResponse> PostXmlTransaction(
            TallyVoucherRequest request,
            CancellationToken cancellationToken = default);

        Task<TallyVoucherResponse> PostJsonTransaction(
            TallyVoucherJsonRequest request,
            CancellationToken cancellationToken = default);
        Task<TallyVoucherApiResponse> CreateSales(
    SalesVoucherRequest request,
    CancellationToken cancellationToken = default);
        Task<TallyVoucherApiResponse> CreateReceipt(
        ReceiptVoucherRequest request,
        CancellationToken cancellationToken = default);
        Task<TallyVoucherApiResponse> CreateSalesOrder(
       SalesOrderVoucherRequest request,
       CancellationToken cancellationToken = default);

        Task<TallyVoucherApiResponse> CreateSalesReturn(
        SalesReturnVoucherRequest request,
        CancellationToken cancellationToken = default);



    }
}