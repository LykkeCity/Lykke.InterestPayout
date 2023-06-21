namespace InterestPayout.Common.Configuration
{
    public class NotificationConfig
    {
        public static NotificationConfig Default => new NotificationConfig() {IsEnabled = false};
        
        public bool IsEnabled { get; set; }
    }
}
