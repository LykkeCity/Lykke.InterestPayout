using System.Threading.Tasks;

namespace InterestPayout.Common.Application
{
    public interface IRecurringPayoutsScheduler
    {
        Task Init();
    }
}
