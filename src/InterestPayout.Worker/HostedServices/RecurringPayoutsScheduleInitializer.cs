using System.Threading;
using System.Threading.Tasks;
using InterestPayout.Common.Application;
using Microsoft.Extensions.Hosting;

namespace InterestPayout.Worker.HostedServices
{
    public class RecurringPayoutsScheduleInitializer : IHostedService
    {
        private readonly IRecurringPayoutsScheduler _recurringPayoutsScheduler;

        public RecurringPayoutsScheduleInitializer(IRecurringPayoutsScheduler recurringPayoutsScheduler)
        {
            _recurringPayoutsScheduler = recurringPayoutsScheduler;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _recurringPayoutsScheduler.Init();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
