using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InterestPayout.Common.Application;
using InterestPayout.Common.Configuration;
using InterestPayout.Common.Extensions;
using InterestPayout.Common.Persistence;
using InterestPayout.Worker.WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swisschain.Extensions.Idempotency;

namespace InterestPayout.Worker.WebApi
{
    [ApiController]
    [Route("api/schedules")]
    public class SchedulesController : ControllerBase
    {
        private readonly IRecurringPayoutsScheduler _payoutsScheduler;
        private readonly IPayoutConfigService _payoutConfigService;

        public SchedulesController(IRecurringPayoutsScheduler payoutsScheduler,
            IPayoutConfigService payoutConfigService)
        {
            _payoutsScheduler = payoutsScheduler;
            _payoutConfigService = payoutConfigService;
        }

        [HttpPost("create-or-update")]
        public async Task<ActionResult> CreateOrUpdate(
            [FromBody] PayoutScheduleCreateOrUpdateRequest createOrUpdateRequest)
        {
            if (createOrUpdateRequest == null)
                return BadRequest("Request is required.");
            if(string.IsNullOrWhiteSpace(createOrUpdateRequest.PayoutAssetId))
                return BadRequest("PayoutAssetId is required.");
            if(string.IsNullOrWhiteSpace(createOrUpdateRequest.AssetId))
                return BadRequest("AssetId is required.");
            if(string.IsNullOrWhiteSpace(createOrUpdateRequest.PayoutCronSchedule))
                return BadRequest("PayoutCronSchedule is required.");
            if (!Quartz.CronExpression.IsValidExpression(createOrUpdateRequest.PayoutCronSchedule))
                return BadRequest("Invalid cron expression.");
            
            var scheduleInterval = new Quartz.CronExpression(createOrUpdateRequest.PayoutCronSchedule).CalculateTimeIntervalBetweenExecutions();
            var minimalAllowedInterval = _payoutConfigService.GetSmallestPayoutScheduleInterval();
            if (scheduleInterval < minimalAllowedInterval)
                throw new InvalidOperationException(
                    $"Scheduled interval for payouts for assetId '{createOrUpdateRequest.AssetId}' is less than minimal allowed interval. Configured value: {scheduleInterval}. Allowed minimum: {minimalAllowedInterval}.");

            await _payoutsScheduler.CreateOrUpdate(new PayoutConfig
            {
                AssetId = createOrUpdateRequest.AssetId,
                PayoutAssetId = createOrUpdateRequest.PayoutAssetId,
                PayoutCronSchedule = createOrUpdateRequest.PayoutCronSchedule,
                Notifications = new NotificationConfig {IsEnabled = createOrUpdateRequest.ShouldNotifyUser}
            });

            return Ok();
        }
        
        [HttpPost("delete")]
        public async Task<ActionResult> Delete([FromBody] IReadOnlyCollection<string> assetIds)
        {
            if (assetIds == null || assetIds.Count == 0 || assetIds.Any(x => string.IsNullOrWhiteSpace(x)))
                return BadRequest("Empty asset IDs.");

            await _payoutsScheduler.Remove(assetIds.ToHashSet());
            
            return Ok();
        }
    }
}
