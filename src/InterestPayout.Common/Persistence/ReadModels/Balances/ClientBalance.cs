using System;

namespace InterestPayout.Common.Persistence.ReadModels.Balances
{
    public class ClientBalance
    {
        public string ClientId { get; set; }
        
        public string WalletId { get; set; }
        
        public WalletType? WalletType { get; set; }
        
        public string AssetId { get; set; }
        public decimal Balance { get; set; }
        public decimal Reserved { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public ClientBalance()
        {
        }

        public ClientBalance(string clientId, string assetId, decimal balance, decimal reserved, DateTime? updatedAt)
        {
            ClientId = clientId;
            AssetId = assetId;
            Balance = balance;
            Reserved = reserved;
            UpdatedAt = updatedAt;
        }
    }
}
