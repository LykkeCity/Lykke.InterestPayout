using System;

namespace InterestPayout.Common.Extensions
{
    public static class CronExpressionExtensions
    {
        /// <summary>
        /// Returns difference in time between two executions of a cron expression.
        /// DOES NOT take into account expressions with irregular intervals, e.g. only on weekdays
        /// </summary>
        public static TimeSpan? CalculateTimeIntervalBetweenExecutions(this Quartz.CronExpression expression)
        {
            var utcNow = DateTimeOffset.Now;
            var validExecutionTime = expression.GetNextValidTimeAfter(utcNow);
            if (!validExecutionTime.HasValue)
                return null;

            var invalidExecutionTimeAfterValid = expression.GetNextInvalidTimeAfter(validExecutionTime.Value);
            if (!invalidExecutionTimeAfterValid.HasValue)
                return null;

            var nextValidExecutionTimeAfterInvalid = expression.GetNextValidTimeAfter(invalidExecutionTimeAfterValid.Value);
            if (!nextValidExecutionTimeAfterInvalid.HasValue)
                return null;

            var differenceBetweenValidExecutionTimesInSeconds =
                nextValidExecutionTimeAfterInvalid.Value.ToUnixTimeSeconds() -
                validExecutionTime.Value.ToUnixTimeSeconds();
            
            return TimeSpan.FromSeconds(differenceBetweenValidExecutionTimesInSeconds);
        }
    }
}
