using System;
using System.Collections.Generic;
using System.Linq;
using InterestPayout.Common.Configuration;
using InterestPayout.Common.Extensions;

namespace InterestPayout.Common.Application
{
    public class PayoutConfigService : IPayoutConfigService
    {
        private readonly TimeSpan _smallestPayoutScheduleInterval;

        public PayoutConfigService(TimeSpan smallestPayoutScheduleInterval)
        {
            _smallestPayoutScheduleInterval = smallestPayoutScheduleInterval;
        }
        
        public TimeSpan GetSmallestPayoutScheduleInterval() => _smallestPayoutScheduleInterval;
    }
}
