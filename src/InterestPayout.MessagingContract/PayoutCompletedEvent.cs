using MessagePack;

namespace Lykke.InterestPayout.MessagingContract
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class PayoutCompletedEvent
    {
        public string OperationId { get; set; }
        
        public string ClientId { get; set; }
        
        public string WalletId { get; set; }
        
        public string AssetId { get; set; }
        
        public string PayoutAssetId { get; set; }
        
        public decimal Amount { get; set; }
    }
}
