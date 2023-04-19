using System;

namespace InterestPayout.Common.Configuration
{
    public class AppConfig
    {
        public DbConfig Db { get; set; }
        
        public ExternalServicesConfig ExternalServices { get; set; }

        public RabbitMqConfig RabbitMq { get; set; }
        
        public PayoutConfig[] Payouts { get; set; }
        
        public TimeSpan SmallestPayoutScheduleInterval { get; set; } = TimeSpan.FromHours(23);
    }
}
