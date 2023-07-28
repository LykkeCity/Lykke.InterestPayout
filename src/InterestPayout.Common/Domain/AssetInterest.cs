using System;

namespace InterestPayout.Common.Domain
{
    public class AssetInterest
    {
        public long Id { get; }
        
        public string AssetId { get; }
        
        public decimal InterestRate { get; }
        
        public DateTimeOffset ValidUntil { get; }
         
        public DateTimeOffset CreatedAt { get; }
        
        public int Version { get; init; }

        private AssetInterest(long id,
            string assetId,
            decimal interestRate,
            DateTimeOffset validUntil,
            int version,
            DateTimeOffset createdAt)
        {
            Id = id;
            AssetId = assetId;
            InterestRate = interestRate;
            ValidUntil = validUntil;
            Version = version;
            CreatedAt = createdAt;
        }

        public static AssetInterest Create(long id,
            string assetId,
            decimal interestRate,
            DateTimeOffset validUntil,
            int version)
        {
            var now = DateTimeOffset.UtcNow;
            return new AssetInterest(id,
                assetId,
                interestRate,
                validUntil,
                version,
                now);
        }
        
        public static AssetInterest Restore(long id,
            string assetId,
            decimal interestRate,
            DateTimeOffset validUntil,
            int version,
            DateTimeOffset createdAt)
        {
            return new AssetInterest(id,
                assetId,
                interestRate,
                validUntil,
                version,
                createdAt);
        }
        
        public AssetInterest CreateNewVersion(long newId,
            decimal newInterestRate,
            DateTimeOffset newValidUntil)
        {
            if (newValidUntil <= ValidUntil)
                throw new InvalidOperationException("New valid until has to be greater than the previous one.");

            return new AssetInterest(newId,
                AssetId,
                newInterestRate,
                newValidUntil,
                Version + 1,
                DateTimeOffset.UtcNow);
        }
    }
}
