using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Table;

namespace InterestPayout.Common.Persistence.ExternalEntities.Wallets
{
    public class WalletEntity : TableEntity
    {
        public static string GeneratePartitionKey()
        {
            return "Wallet";
        }

        public static string GenerateRowKey(string id)
        {
            return id;
        }

        public static IEqualityComparer<WalletEntity> ComparerById { get; } = new EqualityComparerById();

        public DateTime Registered { get; set; }
        public string Id => RowKey;
        
        public string Name { get; set; }

        public string Type { get; set; }

        public string Description { get; set; }

        public string ClientId { get; set; }

        public string Owner { get; set; }

        private class EqualityComparerById : IEqualityComparer<WalletEntity>
        {
            public bool Equals(WalletEntity x, WalletEntity y)
            {
                if (x == y)
                    return true;
                if (x == null || y == null)
                    return false;
                return x.Id == y.Id;
            }

            public int GetHashCode(WalletEntity obj)
            {
                if (obj?.Id == null)
                    return 0;
                return obj.Id.GetHashCode();
            }
        }
    }
}
