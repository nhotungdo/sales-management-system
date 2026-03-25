using SalesManagement.DAL.Entities;

namespace SalesManagement.BLL.Interfaces
{
    public interface IAuthService
    {
        Task<User?> ValidateUser(string username, string password);
        Task<User?> RegisterUser(string username, string email, string password, string? fullName, string? phoneNumber);
    }
}
