using System.Collections.Generic;
using InterestPayout.Common.Configuration;

namespace InterestPayout.Common.Domain
{
    public interface IPayoutConfigService
    {
        IReadOnlyCollection<PayoutConfig> GetAll();
    }
}
