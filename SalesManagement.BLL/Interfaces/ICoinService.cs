using System.Threading.Tasks;

namespace SalesManagement.BLL.Interfaces
{
    public interface ICoinService
    {
        Task<(bool Success, string Message)> UseCoins(string userId, decimal amount);
        decimal CalculateCoin(decimal price);
    }
}