using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Table;

namespace InterestPayout.Common.Persistence.ReadModels.Clients
{
    public class ClientAccountEntity : TableEntity
    {
        public static string GeneratePartitionKey()
        {
            return "Trader";
        }

        public static string GenerateRowKey(string id)
        {
            return id;
        }

        public static IEqualityComparer<ClientAccountEntity> ComparerById { get; } = new EqualityComparerById();

        public DateTime Registered { get; set; }
        public string Id => RowKey;
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Pin { get; set; }
        public string NotificationsId { get; set; }
        public string Salt { get; set; }
        public string Hash { get; set; }
        public string PartnerId { get; set; }
        public bool IsReviewAccount { get; set; }
        public bool IsTrusted { get; set; }
        public bool IsCyprusClient { get; set; }

        private class EqualityComparerById : IEqualityComparer<ClientAccountEntity>
        {
            public bool Equals(ClientAccountEntity x, ClientAccountEntity y)
            {
                if (x == y)
                    return true;
                if (x == null || y == null)
                    return false;
                return x.Id == y.Id;
            }

            public int GetHashCode(ClientAccountEntity obj)
            {
                if (obj?.Id == null)
                    return 0;
                return obj.Id.GetHashCode();
            }
        }
    }
}
