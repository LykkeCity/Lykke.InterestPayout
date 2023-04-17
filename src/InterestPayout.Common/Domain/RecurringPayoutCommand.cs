using System;

namespace InterestPayout.Common.Domain
{
    public class RecurringPayoutCommand
    {
        public string AssetId { get; set; }

        public TimeSpan CronScheduleInterval { get; set; }
        
        public decimal InterestRate { get; set; }
        
        public long InternalScheduleId { get; set; }
    }
}