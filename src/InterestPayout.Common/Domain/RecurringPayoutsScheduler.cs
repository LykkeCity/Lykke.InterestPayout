using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InterestPayout.Common.Configuration;
using InterestPayout.Common.Persistence;
using InterestPayout.Common.Persistence.ReadModels.PayoutSchedules;
using MassTransit;
using MassTransit.Scheduling;
using Microsoft.Extensions.Logging;
using Swisschain.Extensions.Idempotency;

namespace InterestPayout.Common.Domain
{
    public class RecurringPayoutsScheduler : IRecurringPayoutsScheduler
    {
        private readonly IBus _bus;
        private readonly IUnitOfWorkManager<UnitOfWork> _unitOfWorkManager;
        private readonly IPayoutConfigService _payoutConfigService;
        private readonly IIdGenerator _idGenerator;
        private readonly ILogger<RecurringPayoutsScheduler> _logger;

        private const string QueueName = "queue:lykke-interest-payout-recurring-payout-consumer";
        private const string ScheduleGroup = "RecurringInterestPayouts";
        
        public RecurringPayoutsScheduler(IBus bus,
            ILogger<RecurringPayoutsScheduler> logger,
            IUnitOfWorkManager<UnitOfWork> unitOfWorkManager,
            IPayoutConfigService payoutConfigService,
            IIdGenerator idGenerator)
        {
            _bus = bus;
            _logger = logger;
            _unitOfWorkManager = unitOfWorkManager;
            _payoutConfigService = payoutConfigService;
            _idGenerator = idGenerator;
        }
        
        public async Task Init()
        {
            _logger.LogInformation("Initialization of scheduled recurring messages is started");

            var configs = _payoutConfigService.GetAll();
            await CancelSchedulesNotPresentInConfig(configs);
            
            foreach (var config in configs)
            {
                await using var unitOfWork = await _unitOfWorkManager.Begin($"SettingUpSchedules:{config.AssetId}:{DateTimeOffset.Now.Ticks}");
                try
                {
                    await HandleScheduleInitForConfigEntry(config, unitOfWork.PayoutSchedules);
                    await unitOfWork.Commit();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An error occurred during initialization of payout schedule config {@context}", new
                    {
                        config
                    });
                    await TrySafeCancelScheduleAfterError(config, e);
                    throw;
                }
            }
            
            _logger.LogInformation("Initialization of scheduled recurring messages has successfully finished");
        }

        private async Task CancelSchedulesNotPresentInConfig(IReadOnlyCollection<Payout> configs)
        {
            var assets = configs.Select(x => x.AssetId).ToHashSet();
            await using var unitOfWork = await _unitOfWorkManager.Begin($"CancellingSchedulesNotPresentInCfg:{DateTimeOffset.Now.Ticks}");

            var schedulesToBeCancelled = await unitOfWork.PayoutSchedules.GetAllExcept(assets);
            _logger.LogInformation("[CleanUp] found {count} schedules in db which are not present in current settings", schedulesToBeCancelled.Count);
            foreach (var schedule in schedulesToBeCancelled)
            {
                try
                {
                    await RemoveScheduledRecurringPayout(schedule.AssetId);
                    await unitOfWork.PayoutSchedules.Delete(schedule);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An error occurred during removal of payout schedule config {@context}", new
                    {
                        schedule
                    });
                    throw;
                }
            }
            await unitOfWork.Commit();
        }

        private async Task HandleScheduleInitForConfigEntry(Payout config, IPayoutScheduleRepository payoutScheduleRepository)
        {
            var schedule = await payoutScheduleRepository.GetByAssetIdOrDefault(config.AssetId);
            if (schedule == null)
            {
                _logger.LogInformation("[Init]: existing schedule not found, creating new {@context}", new
                {
                    config
                });
                var newScheduleId = await _idGenerator.GetId(config.AssetId, IdGenerators.PayoutSchedules);
                schedule = PayoutSchedule.Create(newScheduleId,
                    config.AssetId,
                    config.PayoutInterestRate,
                    config.PayoutCronSchedule);
                await payoutScheduleRepository.Add(schedule);
                await ScheduleNewRecurringPayout(schedule);
            }
            else
            {
                var hasChanges = schedule.UpdatePayoutSchedule(config.PayoutInterestRate, config.PayoutCronSchedule);
                if (hasChanges)
                {
                    _logger.LogInformation("[Init]: found existing schedule, updating {@context}", new
                    {
                        config
                    });
                    await payoutScheduleRepository.Update(schedule);
                    await RescheduleRecurringPayout(schedule);
                }
                else
                {
                    _logger.LogInformation("[Init]: found existing schedule, no changes, skipping {@context}", new
                    {
                        config
                    });
                }
            }
        }

        private async Task TrySafeCancelScheduleAfterError(Payout config, Exception originalException)
        {
            try
            {
                _logger.LogInformation($"Attempting to cancel schedule payout for asset {config.AssetId} after error");
                await RemoveScheduledRecurringPayout(config.AssetId);
                _logger.LogInformation($"Successfully cancelled schedule for asset {config.AssetId} after error");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "An error occurred during cancellation of payout schedule after error {@context}", new
                {
                    config,
                    OriginalException = originalException,
                });
                throw;
            }
        }

        private async Task ScheduleNewRecurringPayout(PayoutSchedule schedule)
        {
            var cronExpression = new Quartz.CronExpression(schedule.CronSchedule);
            var cronScheduleInterval = cronExpression.GetNextValidTimeAfter(DateTimeOffset.UtcNow) - DateTimeOffset.UtcNow;
            if (!cronScheduleInterval.HasValue)
            {
                var errorMessage = $"Cannot determine next valid scheduled time for cron expression '{schedule.CronSchedule}' for assetId = '{schedule.AssetId}'";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
            
            _logger.LogInformation("Scheduling new recurring message for assetId {@context}", new
            {
                schedule,
                CronScheduleInterval = cronScheduleInterval.Value,
                CronExpressionSummary = cronExpression.GetExpressionSummary()
            });
            
            await _bus.ScheduleRecurringSend(new Uri(QueueName),
                new PayoutRecurringSchedule
                {
                    CronExpression = schedule.CronSchedule,
                    StartTime = DateTimeOffset.UtcNow,
                    EndTime = null,
                    MisfirePolicy = MissedEventPolicy.Skip,
                    ScheduleId = schedule.AssetId,
                    Description = $"Recurring payout command for asset {schedule.AssetId}",
                    ScheduleGroup = ScheduleGroup
                },
                new RecurringPayoutCommand
                {
                    AssetId = schedule.AssetId,
                    CronScheduleInterval = cronScheduleInterval.Value,
                    InterestRate = schedule.InterestRate,
                    InternalScheduleId = schedule.Id
                });
        }

        private async Task RemoveScheduledRecurringPayout(string assetId)
        {
            await _bus.CancelScheduledRecurringSend(assetId, ScheduleGroup);
        }
        
        private async Task RescheduleRecurringPayout(PayoutSchedule schedule)
        {
            await RemoveScheduledRecurringPayout(schedule.AssetId);
            await ScheduleNewRecurringPayout(schedule);
        }
    }
}
