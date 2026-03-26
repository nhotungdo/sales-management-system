using System.Threading.Tasks;

namespace SalesManagement.BLL.Interfaces
{
    public interface ICoinService
    {
        Task<(bool Success, string Message)> UseCoins(string userId, decimal amount, string description = "");
        decimal CalculateCoin(decimal price);
    }
}