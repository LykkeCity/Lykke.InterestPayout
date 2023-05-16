using System;

namespace InterestPayout.Common.Configuration
{
    public class ExternalServiceClientConfig
    {
        public string ServiceUrl { get; set; }
        
        public TimeSpan? Timeout { get; set; }
    }
}
