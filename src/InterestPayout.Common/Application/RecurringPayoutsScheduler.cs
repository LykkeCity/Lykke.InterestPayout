using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InterestPayout.Common.Configuration;
using InterestPayout.Common.Domain;
using InterestPayout.Common.Extensions;
using InterestPayout.Common.Persistence;
using InterestPayout.Common.Persistence.ReadModels.PayoutSchedules;
using MassTransit;
using MassTransit.Scheduling;
using Microsoft.Extensions.Logging;
using Swisschain.Extensions.Idempotency;

namespace InterestPayout.Common.Application
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

        public async Task Remove(ISet<string> assetIds)
        {
            await using var unitOfWork = await _unitOfWorkManager.Begin($"RemovingScheduleEntries:{DateTimeOffset.UtcNow.Ticks}");

            var schedulesToBeCancelled = await unitOfWork.PayoutSchedules.GetAllExcept(assetIds);
            _logger.LogInformation("[Removal] found {count} schedules in db which are going to be cancelled", schedulesToBeCancelled.Count);
            foreach (var schedule in schedulesToBeCancelled)
            {
                try
                {
                    await RemoveScheduledRecurringPayout(schedule.AssetId);
                    await unitOfWork.PayoutSchedules.Delete(schedule);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An error occurred during removal of payout schedule entry {@context}", new
                    {
                        schedule
                    });
                    throw;
                }
            }
            await unitOfWork.Commit();
        }

        public async Task CreateOrUpdate(PayoutConfig config)
        {
            await using var unitOfWork = await _unitOfWorkManager.Begin($"CreateOrUpdateScheduleEntries:{DateTimeOffset.UtcNow.Ticks}");

            var schedule = await unitOfWork.PayoutSchedules.GetByAssetIdOrDefault(config.AssetId);
            if (schedule == null)
            {
                _logger.LogInformation("[Init]: existing schedule not found, creating new {@context}", new
                {
                    config
                });
                var newScheduleId = await _idGenerator.GetId(config.AssetId, IdGenerators.PayoutSchedules);
                schedule = PayoutSchedule.Create(newScheduleId,
                    config.AssetId,
                    config.PayoutAssetId,
                    config.PayoutCronSchedule,
                    config.Notifications.IsEnabled);
                await unitOfWork.PayoutSchedules.Add(schedule);
                await ScheduleNewRecurringPayout(schedule);
            }
            else
            {
                var hasChanges = schedule.UpdatePayoutSchedule(config.PayoutAssetId,
                    config.PayoutCronSchedule,
                    config.Notifications.IsEnabled);
                if (hasChanges)
                {
                    _logger.LogInformation("[Init]: found existing schedule, updating {@context}", new
                    {
                        config
                    });
                    await unitOfWork.PayoutSchedules.Update(schedule);
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

            await unitOfWork.Commit();
        }

        private async Task TrySafeCancelScheduleAfterError(PayoutConfig config, Exception originalException)
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
            var executionInterval = cronExpression.CalculateTimeIntervalBetweenExecutions();
            if (!executionInterval.HasValue)
            {
                var errorMessage = $"Cannot determine execution interval for cron expression '{schedule.CronSchedule}' for assetId = '{schedule.AssetId}'";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
            
            _logger.LogInformation("Scheduling new recurring message for assetId {@context}", new
            {
                schedule,
                CronScheduleInterval = executionInterval.Value,
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
                    PayoutAssetId = schedule.PayoutAssetId,
                    CronScheduleInterval = executionInterval.Value,
                    InternalScheduleId = schedule.Id,
                    InternalScheduleSequence = schedule.Sequence,
                    ShouldNotifyUser = schedule.ShouldNotifyUser
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
