namespace InterestPayout.Common.Configuration
{
    public class AppConfig
    {
        public DbConfig Db { get; set; }

        public RabbitMqConfig RabbitMq { get; set; }
        
        public Payout[] Payouts { get; set; }
    }
}
