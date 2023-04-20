using System;
using Lykke.AzureStorage.Tables;

namespace InterestPayout.Common.Persistence.ExternalEntities.Balances
{
    public class BalanceEntity : AzureTableEntity
    {
        public string WalletId => PartitionKey;
        public string AssetId => RowKey;
        public decimal Balance { get; set; }
        public decimal Reserved { get; set; }
        public DateTime? UpdatedAt { get; set; }

        internal static string GeneratePartitionKey(string walletId) => walletId;
        internal static string GenerateRowKey(string assetId) => assetId;
    }
}
