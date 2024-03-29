using System.Collections.Generic;
using System.Threading.Tasks;
using InterestPayout.Worker.WebApi.Models;
using Lykke.InterestPayout.ApiContract;

namespace Lykke.InterestPayout.ApiClient.WebApi
{
    public interface IAssetInterestsClient
    {
        Task<AssetInterestResponse[]> GetAll();
        Task<bool> CreateOrUpdate(AssetInterestCreateOrUpdateRequest request, string idempotencyId);
        Task<bool> Delete(IReadOnlyCollection<string> assetIds, string idempotencyId);
    }
}
