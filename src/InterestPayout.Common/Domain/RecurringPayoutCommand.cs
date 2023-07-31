using System;

namespace InterestPayout.Common.Domain
{
    public class RecurringPayoutCommand
    {
        public string AssetId { get; set; }
        public string PayoutAssetId { get; set; }

        public TimeSpan CronScheduleInterval { get; set; }
        
        public bool ShouldNotifyUser { get; set; }
        
        public long InternalScheduleId { get; set; }
        
        public int InternalScheduleSequence { get; set; }
    }
}
