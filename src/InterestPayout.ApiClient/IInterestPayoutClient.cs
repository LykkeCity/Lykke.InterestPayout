using Lykke.InterestPayout.ApiContract;

namespace Lykke.InterestPayout.ApiClient
{
    public interface IInterestPayoutClient
    {
        Monitoring.MonitoringClient Monitoring { get; }
    }
}
