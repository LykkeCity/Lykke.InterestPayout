namespace InterestPayout.Common.Configuration
{
    public class RabbitMqConfig
    {
        public string HostUrl { get; set; }
        
        public ushort? HostPort { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public ushort GetHostPortOrDefault() => HostPort ?? 5672;
    }
}
