using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InterestPayout.Common.Domain;
using Microsoft.EntityFrameworkCore;

namespace InterestPayout.Common.Persistence.ReadModels.PayoutSchedules
{
    public class PayoutScheduleRepository : IPayoutScheduleRepository
    {
        private readonly DatabaseContext _dbContext;

        public PayoutScheduleRepository(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Add(PayoutSchedule payoutSchedule)
        {
            var entity = ToEntity(payoutSchedule);
            await _dbContext.PayoutSchedules.AddAsync(entity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task Update(PayoutSchedule payoutSchedule)
        {
            var entity = ToEntity(payoutSchedule);
            _dbContext.PayoutSchedules.Update(entity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task Delete(PayoutSchedule payoutSchedule)
        {
            var entity = ToEntity(payoutSchedule);
            _dbContext.PayoutSchedules.Remove(entity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IReadOnlyCollection<PayoutSchedule>> GetAll()
        {
            var entities = await _dbContext.PayoutSchedules.ToListAsync();

            return entities.ConvertAll(x => ToDomain(x));
        }
        
        public async Task<PayoutSchedule> GetByIdOrDefault(long id)
        {
            var entity = await _dbContext.PayoutSchedules.SingleOrDefaultAsync(x => x.Id == id);
            if (entity == null)
                return null;

            return ToDomain(entity);
        }

        public async Task<IReadOnlyCollection<PayoutSchedule>> GetByIds(ISet<string> assetIds)
        {
            var entries = await _dbContext.PayoutSchedules
                .Where(x => assetIds.Contains(x.AssetId))
                .ToListAsync();

            return entries.ConvertAll(x => ToDomain(x));
        }

        public async Task<PayoutSchedule> GetByAssetIdOrDefault(string assetId)
        {
            var entity = await _dbContext.PayoutSchedules.SingleOrDefaultAsync(x => x.AssetId == assetId);
            if (entity == null)
                return null;

            return ToDomain(entity);
        }

        private static PayoutSchedule ToDomain(PayoutScheduleEntity entity)
        {
            return PayoutSchedule.Restore(entity.Id,
                entity.AssetId,
                entity.PayoutAssetId,
                entity.CronSchedule,
                entity.ShouldNotifyUser,
                entity.CreatedAt,
                entity.UpdatedAt,
                entity.Version,
                entity.Sequence);
        }

        private static PayoutScheduleEntity ToEntity(PayoutSchedule payoutSchedule)
        {
            return new PayoutScheduleEntity
            {
                Id = payoutSchedule.Id,
                AssetId = payoutSchedule.AssetId,
                PayoutAssetId = payoutSchedule.PayoutAssetId,
                CronSchedule = payoutSchedule.CronSchedule,
                ShouldNotifyUser = payoutSchedule.ShouldNotifyUser,
                CreatedAt = payoutSchedule.CreatedAt,
                UpdatedAt = payoutSchedule.UpdatedAt,
                Version = payoutSchedule.Version,
                Sequence = payoutSchedule.Sequence
            };
        }
    }
}
