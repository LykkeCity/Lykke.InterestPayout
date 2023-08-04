using System;

namespace Lykke.InterestPayout.ApiContract
{
    public class AssetInterestResponse
    {
        public long Id { get; set; }
        
        public string AssetId { get; set; }
        
        public decimal InterestRate { get; set; }
        
        public DateTime CreatedAt { get; set;  }
        
        public DateTime UpdatedAt { get; set; }
    }
}
