using Sales_Management.Models;

namespace Sales_Management.Services
{
    public interface IAuthService
    {
        Task<User?> ValidateUser(string username, string password);
        Task<User?> RegisterUser(string username, string email, string password, string? fullName, string? phoneNumber);
        Task CheckInSalesEmployee(int userId);
        Task CheckOutSalesEmployee(int userId, string reason);
    }
}
