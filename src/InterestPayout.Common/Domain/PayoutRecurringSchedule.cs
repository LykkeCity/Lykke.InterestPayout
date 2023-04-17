using System;
using MassTransit.Scheduling;

namespace InterestPayout.Common.Domain
{
    public class PayoutRecurringSchedule : RecurringSchedule
    {
        public string TimeZoneId { get; set; } 
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset? EndTime { get; set; }
        public string ScheduleId { get; set; }
        public string ScheduleGroup { get; set; }
        public string CronExpression { get; set; }
        public string Description { get; set; }
        public MissedEventPolicy MisfirePolicy { get; set; }
    }
}
