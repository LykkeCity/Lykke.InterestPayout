using System.Collections.Generic;
using System.Threading.Tasks;
using InterestPayout.Common.Domain;

namespace InterestPayout.Common.Persistence.ReadModels.PayoutSchedules
{
    public interface IPayoutScheduleRepository
    {
        Task Add(PayoutSchedule payoutSchedule);
        Task Update(PayoutSchedule payoutSchedule);

        Task Delete(PayoutSchedule payoutSchedule);
        
        Task<PayoutSchedule> GetByIdOrDefault(long id);

        Task<IReadOnlyCollection<PayoutSchedule>> GetAllExcept(ISet<string> assetIds);

        Task<PayoutSchedule> GetByAssetIdOrDefault(string assetId);
        Task<IReadOnlyCollection<PayoutSchedule>> GetAll();
    }

}
