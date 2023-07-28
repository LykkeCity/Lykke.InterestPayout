using System;
using System.Linq;
using System.Threading.Tasks;
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

        public async Task Add(Domain.AssetInterest assetInterest)
        {
            var entity = ToEntity(assetInterest);
            await _dbContext.AssetInterests.AddAsync(entity);
        }

        public async Task<Domain.AssetInterest> GetLatestForDateOrDefault(string assetId, DateTimeOffset date)
        {
            var entity = await _dbContext.AssetInterests
                .Where(x => x.AssetId == assetId && x.ValidUntil >= date)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Version)
                .FirstOrDefaultAsync();

            return entity == null ? null : ToDomain(entity);
        }
        
        private static Domain.AssetInterest ToDomain(AssetInterestEntity entity)
        {
            return Domain.AssetInterest.Restore(entity.Id,
                entity.AssetId,
                entity.InterestRate,
                entity.ValidUntil,
                entity.Version,
                entity.CreatedAt);
        }

        private static AssetInterestEntity ToEntity(Domain.AssetInterest interest)
        {
            return new AssetInterestEntity
            {
                Id = interest.Id,
                AssetId = interest.AssetId,
                InterestRate = interest.InterestRate,
                ValidUntil = interest.ValidUntil,
                CreatedAt = interest.CreatedAt,
                Version = interest.Version
            };
        }
    }
}
