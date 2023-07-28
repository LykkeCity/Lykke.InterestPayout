using System;
using System.Threading.Tasks;

namespace InterestPayout.Common.Persistence.ReadModels.AssetInterests
{
    public interface IAssetInterestRepository
    {
        Task Add(Domain.AssetInterest assetInterest);

        Task<Domain.AssetInterest> GetLatestForDateOrDefault(string assetId, DateTimeOffset date);
    }
}
