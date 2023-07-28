using System;

namespace InterestPayout.Worker.WebApi.Models
{
    public class AssetInterestCreateOrUpdateRequest
    {
        public string AssetId { get; set; }
        
        public decimal InterestRate { get; set; }
        
        public DateTimeOffset ValidUntil { get; set; }
    }
}
