using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InterestPayout.Common.Domain;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace InterestPayout.Worker.Messaging.Consumers
{
    public class RecurringPayoutCommandConsumer : IConsumer<RecurringPayoutCommand>
    {
        private readonly ILogger<RecurringPayoutCommandConsumer> _logger;

        public RecurringPayoutCommandConsumer(ILogger<RecurringPayoutCommandConsumer> logger)
        {
            _logger = logger;
        }

        public Task Consume(ConsumeContext<RecurringPayoutCommand> context)
        {
            if (IsMessageExpired(context.Headers, context.Message))
                return Task.CompletedTask;
            
            var headers = new List<string>();
            foreach (var header in context.Headers)
            {
                headers.Add($"key={header.Key}:value={header.Value};");
            }
            //TODO replace with actual implementation
            _logger.LogInformation("recurring message is being handled {@context}", new
            {
                headers
            });
            return Task.CompletedTask;
        }

        private bool IsMessageExpired(Headers headers, RecurringPayoutCommand message)
        {
            var scheduledDateTime = headers.Get<DateTimeOffset>("MT-Quartz-Scheduled");
            if (!scheduledDateTime.HasValue)
                throw new InvalidOperationException("Cannot obtain original scheduled time from the header 'MT-Quartz-Scheduled'");

            // if scheduled time is more than one full interval earlier than current timestamp, then it is too old, skipping
            var currentTimeStamp = DateTimeOffset.UtcNow;
            var latestAcceptableExecutionTimeStamp = scheduledDateTime + message.CronScheduleInterval;
            var needSkip = latestAcceptableExecutionTimeStamp < currentTimeStamp ? "Yes" : "No";
            _logger.LogInformation($"Need skip? {needSkip}. Scheduled: {scheduledDateTime.Value:T}, latestOkExecution: {latestAcceptableExecutionTimeStamp.Value:T}, now: {currentTimeStamp:T}");
            if (latestAcceptableExecutionTimeStamp < currentTimeStamp)
            {
                _logger.LogInformation("recurring message is expired and being skipped {@context}", new
                {
                    headers,
                    currentTimeStamp,
                    latestAcceptableExecutionTimeStamp,
                    CronScheduleIntervalInSeconds = message.CronScheduleInterval.TotalSeconds,
                    message
                });
                return true;
            }
            return false;
        }
    }
}
