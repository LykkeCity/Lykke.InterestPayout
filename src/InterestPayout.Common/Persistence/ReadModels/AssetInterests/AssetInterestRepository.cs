using System.Linq;
using System.Threading.Tasks;
using InterestPayout.Common.Domain;
using Microsoft.EntityFrameworkCore;

namespace InterestPayout.Common.Persistence.ReadModels.AssetInterests
{
    public class AssetInterestRepository : IAssetInterestRepository
    {
        private readonly DatabaseContext _dbContext;

        public AssetInterestRepository(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Add(AssetInterest assetInterest)
        {
            var entity = ToEntity(assetInterest);
            await _dbContext.AssetInterests.AddAsync(entity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task Update(AssetInterest assetInterest)
        {
            var entity = ToEntity(assetInterest);
            _dbContext.AssetInterests.Update(entity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<AssetInterest> GetByAssetOrDefault(string assetId)
        {
            var entity = await _dbContext.AssetInterests
                .Where(x => x.AssetId == assetId)
                .SingleOrDefaultAsync();

            return entity == null ? null : ToDomain(entity);
        }
        
        private static Domain.AssetInterest ToDomain(AssetInterestEntity entity)
        {
            return Domain.AssetInterest.Restore(entity.Id,
                entity.AssetId,
                entity.InterestRate,
                entity.Version,
                entity.Sequence,
                entity.CreatedAt,
                entity.UpdatedAt);
        }

        private static AssetInterestEntity ToEntity(AssetInterest interest)
        {
            return new AssetInterestEntity
            {
                Id = interest.Id,
                AssetId = interest.AssetId,
                InterestRate = interest.InterestRate,
                CreatedAt = interest.CreatedAt,
                UpdatedAt = interest.UpdatedAt,
                Version = interest.Version,
                Sequence = interest.Sequence,
            };
        }
    }
}
