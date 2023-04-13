using System;
using Grpc.Net.Client;
using Lykke.InterestPayout.ApiContract;

namespace Lykke.InterestPayout.ApiClient
{
    public class InterestPayoutClient : IInterestPayoutClient, IDisposable
    {
        private readonly GrpcChannel _channel;

        public InterestPayoutClient(string serverGrpcUrl)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            _channel = GrpcChannel.ForAddress(serverGrpcUrl);

            Monitoring = new Monitoring.MonitoringClient(_channel);
        }

        public Monitoring.MonitoringClient Monitoring { get; }

        public void Dispose()
        {
            _channel?.Dispose();
        }
    }
}
