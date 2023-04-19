using System;

namespace InterestPayout.Common.Domain
{
    public class PayoutSchedule
    {
        public long Id { get; }
        public string AssetId { get; }
        public decimal InterestRate { get; private set; }
        public string CronSchedule { get; private set; }
        
        public int Accuracy { get; private set; }
        public DateTimeOffset CreatedAt { get; }
        public DateTimeOffset UpdatedAt { get; private set; }
        public uint Version { get; }
        public int Sequence { get; private set; }

        private PayoutSchedule(long id,
            string assetId,
            decimal interestRate,
            string cronSchedule,
            int accuracy,
            DateTimeOffset createdAt,
            DateTimeOffset updatedAt,
            uint version,
            int sequence)
        {
            Id = id;
            AssetId = assetId;
            InterestRate = interestRate;
            CronSchedule = cronSchedule;
            Accuracy = accuracy;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            Version = version;
            Sequence = sequence;
        }

        public static PayoutSchedule Create(long id,
            string assetId,
            decimal interestRate,
            string cronSchedule,
            int accuracy)
        {
            var now = DateTimeOffset.UtcNow;
            return new PayoutSchedule(id,
                assetId,
                interestRate,
                cronSchedule,
                accuracy,
                createdAt: now,
                updatedAt: now,
                version: default,
                sequence: 0);
        }

        public static PayoutSchedule Restore(long id,
            string assetId,
            decimal interestRate,
            string cronSchedule,
            int accuracy,
            DateTimeOffset createdAt,
            DateTimeOffset updatedAt,
            uint version,
            int sequence)
        {
            return new PayoutSchedule(id,
                assetId,
                interestRate,
                cronSchedule,
                accuracy,
                createdAt,
                updatedAt,
                version,
                sequence);
        }

        public bool UpdatePayoutSchedule(decimal newInterestRate, string newCronSchedule, int accuracy)
        {
            var hasChanges = false;
            if (newInterestRate != InterestRate)
            {
                InterestRate = newInterestRate;
                hasChanges = true;
            }
            
            if (accuracy != Accuracy)
            {
                Accuracy = accuracy;
                hasChanges = true;
            }

            if (!CronSchedule.Equals(newCronSchedule))
            {
                CronSchedule = newCronSchedule;
                hasChanges = true;
            }

            if (hasChanges)
            {
                Sequence++;
                UpdatedAt = DateTimeOffset.UtcNow;
            }

            return hasChanges;
        }
    }
}
