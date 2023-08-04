using System.Collections.Generic;
using System.Threading.Tasks;
using InterestPayout.Worker.WebApi.Models;
using Lykke.InterestPayout.ApiContract;

namespace Lykke.InterestPayout.ApiClient.WebApi
{
    public interface IPayoutSchedulesClient
    {
        Task<bool> CreateOrUpdate(PayoutScheduleCreateOrUpdateRequest request, string idempotencyId);
        Task<bool> Delete(IReadOnlyCollection<string> assetIds, string idempotencyId);
        Task<PayoutScheduleResponse[]> GetAll();
    }
}
