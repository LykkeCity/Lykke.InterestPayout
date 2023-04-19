namespace InterestPayout.Common.Configuration
{
    public class ExternalServicesConfig
    {
        public string WalletsConnectionString { get; set; }
        
        public string ClientPersonalInfoConnectionString { get; set; }
        
        public string BalancesConnectionString { get; set; }
        
        public string MatchingEngineConnectionString { get; set; }
        
        public AssetsServiceClientConfig AssetsService { get; set; }
    }
}
