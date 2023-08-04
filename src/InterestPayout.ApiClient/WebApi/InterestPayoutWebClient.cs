using System.Net.Http;

namespace Lykke.InterestPayout.ApiClient.WebApi
{
    public class InterestPayoutWebClient : IInterestPayoutWebClient
    {
        public InterestPayoutWebClient(HttpClient client, string baseUrl)
        {
            AssetInterests = new AssetInterestsClient(client, baseUrl);
            PayoutSchedules = new PayoutSchedulesClient(client, baseUrl);
        }
        
        public IAssetInterestsClient AssetInterests { get; }
        public IPayoutSchedulesClient PayoutSchedules { get; }
    }
}
