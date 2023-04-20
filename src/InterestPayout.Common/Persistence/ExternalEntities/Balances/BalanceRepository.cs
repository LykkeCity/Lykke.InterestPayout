using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;

namespace InterestPayout.Common.Persistence.ExternalEntities.Balances
{
    public class BalanceRepository : IBalanceRepository
    {
        private readonly INoSQLTableStorage<BalanceEntity> _balances;

        public BalanceRepository(INoSQLTableStorage<BalanceEntity> balances)
        {
            _balances = balances;
        }

        public async Task<IReadOnlyCollection<ClientBalance>> GetBalances(string clientId,
            IReadOnlyCollection<string> walletIds,
            string assetId)
        {
            var allWallets = (walletIds ?? Array.Empty<string>()).Concat(
                string.IsNullOrWhiteSpace(clientId)
                    ? Array.Empty<string>()
                    : new[] {clientId});
            var balances = new List<ClientBalance>();
            foreach (var walletId in allWallets)
            {
                try
                {
                    var balance = await _balances.GetDataAsync(walletId);

                    balances.AddRange(balance
                        .Where(x => (x.Balance > decimal.Zero || x.Reserved > decimal.Zero) && x.AssetId == assetId)
                        .Select(x => new ClientBalance(clientId, x.AssetId, x.Balance, x.Reserved, x.UpdatedAt)
                        {
                            WalletId = walletId,
                            WalletType = walletId == clientId ? WalletType.Trading : WalletType.Trusted
                        })
                        .ToList());
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error getting balances for client Id = '{clientId}': {ex.Message}", ex);
                }
            }

            return balances;
        }
    }
}
