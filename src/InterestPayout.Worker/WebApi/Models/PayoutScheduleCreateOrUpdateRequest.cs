namespace InterestPayout.Worker.WebApi.Models
{
    public class PayoutScheduleCreateOrUpdateRequest
    {
        public string AssetId { get; set; }
        
        public string PayoutAssetId { get; set; }
        
        public string PayoutCronSchedule { get; set; }
        
        public bool ShouldNotifyUser { get; set; }
    }
}
