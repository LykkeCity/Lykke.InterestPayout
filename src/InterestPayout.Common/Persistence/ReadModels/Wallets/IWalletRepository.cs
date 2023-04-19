using System.Collections.Generic;
using System.Threading.Tasks;

namespace InterestPayout.Common.Persistence.ReadModels.Wallets
{
    public interface IWalletRepository
    {
        Task<IReadOnlyCollection<WalletEntity>> GetAllByClient(string clientId);
    }
}
