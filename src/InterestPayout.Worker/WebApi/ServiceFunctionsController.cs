using System;
using System.Linq;
using System.Threading.Tasks;
using InterestPayout.Common.Domain;
using InterestPayout.Common.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swisschain.Extensions.Idempotency;

namespace InterestPayout.Worker.WebApi
{
    [ApiController]
    [Route("api/service-functions")]
    public class ServiceFunctionsController : ControllerBase
    {
        private readonly IUnitOfWorkManager<UnitOfWork> _unitOfWorkManager;
        private readonly ILogger<RecurringPayoutsScheduler> _logger;
        private readonly IRecurringPayoutsScheduler _payoutsScheduler;

        public ServiceFunctionsController(IUnitOfWorkManager<UnitOfWork> unitOfWorkManager,
            ILogger<RecurringPayoutsScheduler> logger,
            IRecurringPayoutsScheduler payoutsScheduler)
        {
            _unitOfWorkManager = unitOfWorkManager;
            _logger = logger;
            _payoutsScheduler = payoutsScheduler;
        }

        [HttpGet("re-init-schedules")]
        public async Task<ActionResult> ReInitSchedulesAsync()
        {
            await using var unitOfWork = await _unitOfWorkManager.Begin($"ReInitSchedules:{DateTimeOffset.Now.Ticks}");
            var persistedSchedules = await unitOfWork.PayoutSchedules.GetAll();
            if (!persistedSchedules.Any())
            {
                _logger.LogInformation($"Persisted schedules not found. Exiting...");
                return Ok(0);
            }
            _logger.LogInformation($"Found {persistedSchedules.Count} schedules. Purging...");

            foreach (var persistedSchedule in persistedSchedules)
            {
                _logger.LogInformation("Purging schedule {@context}", new
                {
                    schedule = persistedSchedule
                });
                await unitOfWork.PayoutSchedules.Delete(persistedSchedule);
            }

            await unitOfWork.Commit();

            await _payoutsScheduler.Init();

            return Ok(persistedSchedules.Count);
        }
    }
}
