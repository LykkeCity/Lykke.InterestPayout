namespace InterestPayout.Common.Configuration
{
    public sealed class AzureConfig
    {
        public string LogConnectionString { get; set; }
        
        public string SlackConnString { get; set; }
        
        public string SlackNotificationsQueue { get; set; }
    }
}
