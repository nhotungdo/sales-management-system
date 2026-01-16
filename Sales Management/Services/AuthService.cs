using Microsoft.EntityFrameworkCore;
using Sales_Management.Data;
using Sales_Management.Models;
using BCrypt.Net;

namespace Sales_Management.Services
{
    public class AuthService : IAuthService
    {
        private readonly SalesManagementContext _context;

        public AuthService(SalesManagementContext context)
        {
            _context = context;
        }

        public async Task<User?> ValidateUser(string username, string password)
        {
            // Tìm user theo username
            var user = await _context.Users
                .FirstOrDefaultAsync(u => 
                    u.Username == username && 
                    u.IsActive && 
                    !u.IsDeleted);

            if (user == null) return null;

            // Verify password với BCrypt
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return null;

            // Update last login
            user.LastLogin = DateTime.Now;
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<User?> RegisterUser(
            string username, 
            string email, 
            string password, 
            string? fullName,
            string? phoneNumber)
        {
            // Kiểm tra username đã tồn tại
            if (await _context.Users.AnyAsync(u => u.Username == username))
                return null;

            // Kiểm tra email đã tồn tại
            if (await _context.Users.AnyAsync(u => u.Email == email))
                return null;

            // Tạo user mới
            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password), // Hash password với BCrypt
                FullName = fullName,
                PhoneNumber = phoneNumber,
                Role = "Customer", // Mặc định là Customer
                IsActive = true,
                IsDeleted = false,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }
    }
}
