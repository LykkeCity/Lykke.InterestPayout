namespace Lykke.InterestPayout.ApiClient.WebApi
{
    public interface IInterestPayoutWebClient
    {
        IAssetInterestsClient AssetInterests { get; }
        
        IPayoutSchedulesClient PayoutSchedules { get; }
    }
}
