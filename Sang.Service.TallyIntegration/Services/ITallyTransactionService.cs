using Sang.Service.TallyIntegration.Models;

namespace Sang.Service.TallyIntegration.Services
{
    public interface ITallyTransactionService
    {
        Task<TallyVoucherResponse> PostTransaction(
           TallyVoucherRequest request,
           CancellationToken cancellationToken = default);
    }
}
