using InterestPayout.Common.Persistence.ReadModels.AssetInterests;
using InterestPayout.Common.Persistence.ReadModels.PayoutSchedules;
using Swisschain.Extensions.Idempotency.EfCore;

namespace InterestPayout.Common.Persistence
{
    public class UnitOfWork : UnitOfWorkBase<DatabaseContext>
    {
        protected override void ProvisionRepositories(DatabaseContext dbContext)
        {
            PayoutSchedules = new PayoutScheduleRepository(dbContext);
            AssetInterests = new AssetInterestRepository(dbContext);
        }
        
        public IPayoutScheduleRepository PayoutSchedules { get; private set; }
        
        public IAssetInterestRepository AssetInterests { get; private set; }
    }
}
