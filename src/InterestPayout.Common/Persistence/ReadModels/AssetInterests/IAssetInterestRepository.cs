using System.Collections.Generic;
using System.Threading.Tasks;

namespace InterestPayout.Common.Persistence.ReadModels.AssetInterests
{
    public interface IAssetInterestRepository
    {
        Task Add(Domain.AssetInterest assetInterest);
        
        Task Update(Domain.AssetInterest assetInterest);
        
        Task<int> DeleteByAssetIds(IReadOnlyCollection<string> assetIds);

        Task<Domain.AssetInterest> GetByAssetOrDefault(string assetId);

        Task<IReadOnlyCollection<Domain.AssetInterest>> GetAll();
    }
}
