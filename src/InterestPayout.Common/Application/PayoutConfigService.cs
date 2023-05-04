using System;
using System.Collections.Generic;
using System.Linq;
using InterestPayout.Common.Configuration;
using InterestPayout.Common.Extensions;

namespace InterestPayout.Common.Application
{
    public class PayoutConfigService : IPayoutConfigService
    {
        private readonly PayoutConfig[] _configs;
        private readonly TimeSpan _smallestPayoutScheduleInterval;

        public PayoutConfigService(PayoutConfig[] configs, TimeSpan smallestPayoutScheduleInterval)
        {
            _smallestPayoutScheduleInterval = smallestPayoutScheduleInterval;
            _configs = configs;
            ValidateConfigs(configs);
        }

        public IReadOnlyCollection<PayoutConfig> GetAll()
        {
            return _configs;
        }

        private void ValidateConfigs(PayoutConfig[] configs)
        {
            if (configs == null)
                throw new InvalidOperationException("Payouts were not specified in the configuration.");

            var duplicates = configs
                .GroupBy(x => x.AssetId)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToArray();

            if (duplicates.Any())
                throw new InvalidOperationException(
                    $"Payout settings are duplicate for the following assetIds: {string.Join(',', duplicates)}.");

            foreach (var config in configs)
            {
                if (string.IsNullOrWhiteSpace(config.PayoutAssetId))
                    throw new InvalidOperationException($"Payout asset ID should be specified for the assetId {config.AssetId}");

                if (!Quartz.CronExpression.IsValidExpression(config.PayoutCronSchedule))
                    throw new InvalidOperationException($"Invalid cron expression ('{config.PayoutCronSchedule}') for assetId '{config.AssetId}'.");

                if (config.PayoutInterestRate < -100)
                    throw new InvalidOperationException($"Interest rate cannot be less than minus one hundred percent, but was '{config.PayoutInterestRate}' for assetId '{config.AssetId}'.");

                var scheduleInterval = new Quartz.CronExpression(config.PayoutCronSchedule).CalculateTimeIntervalBetweenExecutions();
                if (scheduleInterval < _smallestPayoutScheduleInterval)
                    throw new InvalidOperationException(
                        $"Scheduled interval for payouts for assetId '{config.AssetId}' is less than minimal allowed interval. Configured value: {scheduleInterval}. Allowed minimum: {_smallestPayoutScheduleInterval}.");
            }
        }
    }
}
