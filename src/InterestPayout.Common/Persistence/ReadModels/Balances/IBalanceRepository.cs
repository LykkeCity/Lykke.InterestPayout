using System.Collections.Generic;
using System.Threading.Tasks;

namespace InterestPayout.Common.Persistence.ReadModels.Balances
{
    public interface IBalanceRepository
    {
        Task<IReadOnlyCollection<ClientBalance>> GetBalances(string clientId,
            IReadOnlyCollection<string> walletIds,
            string assetId);
    }
}
