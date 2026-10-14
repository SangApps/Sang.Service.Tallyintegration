using Sang.Service.TallyIntegration.Models;

namespace Sang.Service.TallyIntegration.Services
{
    public interface ITallyService
    {
        Task<List<TallyLedgerDto>> GetLedgers(
            CancellationToken cancellationToken = default);

        Task<List<TallyMasterDto>> GetProducts(
            CancellationToken cancellationToken = default);

        Task<List<TallyMasterDto>> GetWarehouses(
            CancellationToken cancellationToken = default);

        Task<List<TallyMasterDto>> GetUnits(
         CancellationToken cancellationToken = default);

        Task<string> GetCompanyDetails(
            CancellationToken cancellationToken = default);

        Task<string> GetVouchers(
            CancellationToken cancellationToken = default);

        Task<string> CreateLedger(
            string name,
            string parent,
            CancellationToken cancellationToken = default);

        Task<string> CreateProduct(
            string name,
            string parent,
            string baseUnits,
            CancellationToken cancellationToken = default);

        Task<TallyVoucherResponse> PostTransaction(
            TallyVoucherRequest request,
            CancellationToken cancellationToken = default);
    }
}