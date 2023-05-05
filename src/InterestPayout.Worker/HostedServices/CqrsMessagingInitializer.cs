using System.Threading;
using System.Threading.Tasks;
using Lykke.Cqrs;
using Microsoft.Extensions.Hosting;

namespace InterestPayout.Worker.HostedServices
{
    public class CqrsMessagingInitializer : IHostedService
    {
        private readonly ICqrsEngine _cqrsEngine;

        public CqrsMessagingInitializer( ICqrsEngine cqrsEngine)
        {
            _cqrsEngine = cqrsEngine;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cqrsEngine.StartPublishers();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
