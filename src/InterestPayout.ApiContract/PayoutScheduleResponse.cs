using System;

namespace Lykke.InterestPayout.ApiContract
{
    public class PayoutScheduleResponse
    {
        public long Id { get; set; }
        public string AssetId { get; set; }
        public string PayoutAssetId { get; set; }
        public string CronSchedule { get; set; }
        public bool ShouldNotifyUser { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
