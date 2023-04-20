using System.Collections.Generic;
using System.Threading.Tasks;

namespace InterestPayout.Common.Persistence.ExternalEntities.Wallets
{
    public interface IWalletRepository
    {
        Task<IReadOnlyCollection<WalletEntity>> GetAllByClient(string clientId);
    }
}
