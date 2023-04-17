using System;
using System.Collections.Generic;
using System.Linq;
using InterestPayout.Common.Configuration;

namespace InterestPayout.Common.Domain
{
    public class PayoutConfigService : IPayoutConfigService
    {
        private readonly PayoutConfig[] _configs;

        public PayoutConfigService(PayoutConfig[] configs)
        {
            ValidateConfigs(configs);
            _configs = configs;
        }

        public IReadOnlyCollection<PayoutConfig> GetAll()
        {
            return _configs;
        }

        private static void ValidateConfigs(PayoutConfig[] configs)
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
                    $"Payouts settings are duplicate for the following assetIds: {string.Join(',', duplicates)}.");

            foreach (var config in configs)
            {
                if (!Quartz.CronExpression.IsValidExpression(config.PayoutCronSchedule))
                    throw new InvalidOperationException($"Invalid cron expression ('{config.PayoutCronSchedule}') for assetId '{config.AssetId}'.");
            }
        }
    }
}
