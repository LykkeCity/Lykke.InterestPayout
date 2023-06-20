namespace InterestPayout.Common.Configuration
{
    public class PayoutConfig
    {
        public string AssetId { get; set; }
        public string PayoutAssetId { get; set; }
        public string PayoutCronSchedule { get; set; }
        public decimal PayoutInterestRate { get; set; }
        
        public NotificationConfig Notifications { get; set; } = NotificationConfig.Default;
    }
}
