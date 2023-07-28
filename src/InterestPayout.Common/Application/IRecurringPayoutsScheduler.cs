using System.Collections.Generic;
using System.Threading.Tasks;
using InterestPayout.Common.Configuration;

namespace InterestPayout.Common.Application
{
    public interface IRecurringPayoutsScheduler
    {
        Task CreateOrUpdate(PayoutConfig payoutConfig);
        
        Task Remove(ISet<string> assetIds);
    }
}
