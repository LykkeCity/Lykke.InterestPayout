using System;
using System.Collections.Generic;
using InterestPayout.Common.Configuration;

namespace InterestPayout.Common.Application
{
    public interface IPayoutConfigService
    {
        TimeSpan GetSmallestPayoutScheduleInterval();
    }
}
