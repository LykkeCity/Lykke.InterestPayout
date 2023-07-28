using System;

namespace InterestPayout.Common.Persistence.ReadModels.AssetInterests
{
    public class AssetInterestEntity
    {
        public long Id { get; set; }
        
        public string AssetId { get; set; }
        
        public decimal InterestRate { get; set; }
        
        public DateTimeOffset CreatedAt { get; set; }
        
        public int Version { get; set; }
        
        public DateTimeOffset ValidUntil { get; set; }
    }
}
