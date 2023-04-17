namespace InterestPayout.Common.Configuration
{
    public class Payout
    {
        public string AssetId { get; set; }
        public string PayoutCronSchedule { get; set; }
        public decimal PayoutInterestRate { get; set; }
    }
}
