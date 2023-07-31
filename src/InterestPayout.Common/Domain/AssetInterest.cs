using System;

namespace InterestPayout.Common.Domain
{
    public class AssetInterest
    {
        public long Id { get; }
        
        public string AssetId { get; }
        
        public decimal InterestRate { get; private set; }
        
        public DateTime CreatedAt { get; }
        
        public DateTime UpdatedAt { get; private set; }
        
        public uint Version { get; private set; }
        
        public int Sequence { get; private set; }

        private AssetInterest(long id,
            string assetId,
            decimal interestRate,
            uint version,
            int sequence,
            DateTime createdAt,
            DateTime updatedAt)
        {
            Id = id;
            AssetId = assetId;
            InterestRate = interestRate;
            Version = version;
            Sequence = sequence;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        public static AssetInterest Create(long id,
            string assetId,
            decimal interestRate)
        {
            var now = DateTime.UtcNow;
            return new AssetInterest(id,
                assetId,
                interestRate,
                default,
                default,
                now,
                now);
        }
        
        public static AssetInterest Restore(long id,
            string assetId,
            decimal interestRate,
            uint version,
            int sequence,
            DateTime createdAt,
            DateTime updatedAt)
        {
            return new AssetInterest(id,
                assetId,
                interestRate,
                version,
                sequence,
                createdAt,
                updatedAt);
        }
        
        public bool UpdateInterestRate(decimal newInterestRate)
        {
            if (InterestRate == newInterestRate)
                return false;

            InterestRate = newInterestRate;
            Sequence++;
            UpdatedAt = DateTime.UtcNow;
            return true;
        }
    }
}
