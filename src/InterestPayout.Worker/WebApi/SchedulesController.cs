using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
            [FromBody] PayoutScheduleCreateOrUpdateRequest request,
            [Required, FromHeader(Name = "X-Idempotency-ID")] string idempotencyId)
        {
            if (request == null)
                return BadRequest("Request is required.");
            if(string.IsNullOrWhiteSpace(request.PayoutAssetId))
                return BadRequest("PayoutAssetId is required.");
            if(string.IsNullOrWhiteSpace(request.AssetId))
                return BadRequest("AssetId is required.");
            if(string.IsNullOrWhiteSpace(request.PayoutCronSchedule))
                return BadRequest("PayoutCronSchedule is required.");
            if (!Quartz.CronExpression.IsValidExpression(request.PayoutCronSchedule))
                return BadRequest("Invalid cron expression.");
            
            var scheduleInterval = new Quartz.CronExpression(request.PayoutCronSchedule).CalculateTimeIntervalBetweenExecutions();
            var minimalAllowedInterval = _payoutConfigService.GetSmallestPayoutScheduleInterval();
            if (scheduleInterval < minimalAllowedInterval)
                throw new InvalidOperationException(
                    $"Scheduled interval for payouts for assetId '{request.AssetId}' is less than minimal allowed interval. Configured value: {scheduleInterval}. Allowed minimum: {minimalAllowedInterval}.");

            await _payoutsScheduler.CreateOrUpdate(new PayoutConfig
            {
                AssetId = request.AssetId,
                PayoutAssetId = request.PayoutAssetId,
                PayoutCronSchedule = request.PayoutCronSchedule,
                Notifications = new NotificationConfig {IsEnabled = request.ShouldNotifyUser}
            },
            idempotencyId);

            return Ok();
        }
        
        [HttpDelete("delete")]
        public async Task<ActionResult> Delete(
            [FromBody] IReadOnlyCollection<string> assetIds,
            [Required, FromHeader(Name = "X-Idempotency-ID")] string idempotencyId)
        {
            if (assetIds == null || assetIds.Count == 0 || assetIds.Any(x => string.IsNullOrWhiteSpace(x)))
                return BadRequest("Empty asset IDs.");

            await _payoutsScheduler.Remove(assetIds.ToHashSet(), idempotencyId);
            
            return Ok();
        }
    }
}
