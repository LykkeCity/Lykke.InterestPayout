using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;

namespace InterestPayout.Common.Persistence.ReadModels.Wallets
{
    public class WalletRepository : IWalletRepository
    {
        private readonly INoSQLTableStorage<WalletEntity> _walletsStorage;

        public WalletRepository(INoSQLTableStorage<WalletEntity> walletsStorage)
        {
            _walletsStorage = walletsStorage;
        }
        
        public async Task<IReadOnlyCollection<WalletEntity>> GetAllByClient(string clientId)
        {
            try
            {
                var wallets = await _walletsStorage.GetDataAsync(clientId);
                return wallets.ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error getting wallet for client Id = '{clientId}': {ex.Message}", ex);
            }
        }
    }
}
