namespace InterestPayout.Common.Configuration
{
    public class ExternalServicesConfig
    {
        public ExternalServiceClientConfig ClientAccountService { get; set; }
        
        public ExternalServiceClientConfig BalancesService { get; set; }
        
        public string MatchingEngineConnectionString { get; set; }
        
        public ExternalServiceClientConfig AssetsService { get; set; }
    }
}
