using System;

namespace InterestPayout.Common.Persistence.ReadModels.AssetInterests
{
    public class AssetInterestEntity
    {
        public long Id { get; set; }
        
        public string AssetId { get; set; }
        
        public decimal InterestRate { get; set; }
        
        public uint Version { get; set; }
        
        public int Sequence { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
    }
}
