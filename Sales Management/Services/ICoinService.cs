using System.Threading.Tasks;

namespace Sales_Management.Services
{
    public interface ICoinService
    {
        Task<bool> UseCoins(string userId, decimal amount);

        decimal CalculateCoin(decimal price);
    }
}