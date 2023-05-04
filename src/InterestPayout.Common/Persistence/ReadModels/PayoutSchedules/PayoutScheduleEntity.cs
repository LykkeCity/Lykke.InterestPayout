using System;

namespace InterestPayout.Common.Persistence.ReadModels.PayoutSchedules
{
    public class PayoutScheduleEntity
    {
        public long Id { get; set; }
        
        public string AssetId { get; set; }

        public string PayoutAssetId { get; set; }
        
        public decimal InterestRate { get; set; }
        
        public string CronSchedule { get; set; }
        
        public DateTimeOffset CreatedAt { get; set; }
        
        public DateTimeOffset UpdatedAt { get; set; }
        
        public uint Version { get; set; }
        
        public int Sequence { get; set; }
    }
}
