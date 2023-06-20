using System;

namespace InterestPayout.Common.Domain
{
    public class PayoutSchedule
    {
        public long Id { get; }
        public string AssetId { get; }
        public string PayoutAssetId { get; private set; }
        public decimal InterestRate { get; private set; }
        public string CronSchedule { get; private set; }
        
        public bool ShouldNotifyUser { get; set; }
        
        public DateTimeOffset CreatedAt { get; }
        public DateTimeOffset UpdatedAt { get; private set; }
        public uint Version { get; }
        public int Sequence { get; private set; }

        private PayoutSchedule(long id,
            string assetId,
            string payoutAssetId,
            decimal interestRate,
            string cronSchedule,
            bool shouldNotifyUser,
            DateTimeOffset createdAt,
            DateTimeOffset updatedAt,
            uint version,
            int sequence)
        {
            Id = id;
            AssetId = assetId;
            PayoutAssetId = payoutAssetId;
            InterestRate = interestRate;
            CronSchedule = cronSchedule;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            Version = version;
            Sequence = sequence;
        }

        public static PayoutSchedule Create(long id,
            string assetId,
            string payoutAssetId,
            decimal interestRate,
            string cronSchedule,
            bool shouldNotifyUser)
        {
            var now = DateTimeOffset.UtcNow;
            return new PayoutSchedule(id,
                assetId,
                payoutAssetId,
                interestRate,
                cronSchedule,
                shouldNotifyUser,
                createdAt: now,
                updatedAt: now,
                version: default,
                sequence: 0);
        }

        public static PayoutSchedule Restore(long id,
            string assetId,
            string payoutAssetId,
            decimal interestRate,
            string cronSchedule,
            bool shouldNotifyUser,
            DateTimeOffset createdAt,
            DateTimeOffset updatedAt,
            uint version,
            int sequence)
        {
            return new PayoutSchedule(id,
                assetId,
                payoutAssetId,
                interestRate,
                cronSchedule,
                shouldNotifyUser,
                createdAt,
                updatedAt,
                version,
                sequence);
        }

        public bool UpdatePayoutSchedule(string newPayoutAssetId,
            decimal newInterestRate,
            string newCronSchedule,
            bool newShouldNotifyUser)
        {
            var hasChanges = false;

            if(newPayoutAssetId != PayoutAssetId)
            {
                PayoutAssetId = newPayoutAssetId;
                hasChanges = true;
            }

            if (newInterestRate != InterestRate)
            {
                InterestRate = newInterestRate;
                hasChanges = true;
            }
            
            if (!CronSchedule.Equals(newCronSchedule))
            {
                CronSchedule = newCronSchedule;
                hasChanges = true;
            }
            
            if (ShouldNotifyUser != newShouldNotifyUser)
            {
                ShouldNotifyUser = newShouldNotifyUser;
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
