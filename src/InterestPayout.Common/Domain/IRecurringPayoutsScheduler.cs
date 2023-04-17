using System.Threading.Tasks;

namespace InterestPayout.Common.Domain
{
    public interface IRecurringPayoutsScheduler
    {
        Task Init();
    }
}
