namespace InterestPayout.Common.Configuration
{
    public class RabbitMqConfig
    {
        public string CqrsConnString { get; set; }
        
        public string HostUrl { get; set; }
        
        public ushort? Port { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }
    }
}
