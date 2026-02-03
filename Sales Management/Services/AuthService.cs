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
            bool isValid = false;
            try
            {
                isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                // Fallback: Check if password was stored as plain text
                if (user.PasswordHash == password)
                {
                    isValid = true;
                    // Auto-heal: Update to BCrypt hash
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                }
            }

            if (!isValid) return null;

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

            // Auto-create Customer profile
            var customer = new Customer
            {
                UserId = user.UserId,
                FullName = user.FullName ?? user.Username,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                CreatedDate = DateTime.Now,
                Type = "Personal",
                CustomerLevel = "Regular"
            };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return user;
        }
        public async Task CheckInSalesEmployee(int userId)
        {
            var user = await _context.Users
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null || user.Role != "Sales" || user.Employee == null)
                return;

            var today = DateOnly.FromDateTime(DateTime.Now);
            
            // Check if there is already an active session (not checked out)
            var activeSession = await _context.TimeAttendances
                .FirstOrDefaultAsync(t => 
                    t.EmployeeId == user.Employee.EmployeeId && 
                    t.Date == today && 
                    t.CheckOutTime == null);

            if (activeSession == null)
            {
                var attendance = new TimeAttendance
                {
                    EmployeeId = user.Employee.EmployeeId,
                    Date = today,
                    CheckInTime = DateTime.Now,
                    Status = "Present",
                    Platform = "Web"
                };
                _context.TimeAttendances.Add(attendance);
                await _context.SaveChangesAsync();
            }
        }

        public async Task CheckOutSalesEmployee(int userId, string reason)
        {
            var user = await _context.Users
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null || user.Role != "Sales" || user.Employee == null)
                return;

            var today = DateOnly.FromDateTime(DateTime.Now);

            // Find the active session to close
            var activeSession = await _context.TimeAttendances
                .OrderByDescending(t => t.CheckInTime)
                .FirstOrDefaultAsync(t => 
                    t.EmployeeId == user.Employee.EmployeeId && 
                    t.CheckOutTime == null);

            if (activeSession != null)
            {
                activeSession.CheckOutTime = DateTime.Now;
                
                if (string.IsNullOrWhiteSpace(reason))
                {
                    activeSession.Status = "Check-out"; // As per requirement for no reason provided
                    activeSession.Notes = "No reason provided";
                }
                else
                {
                    activeSession.Notes = reason;
                }
                
                await _context.SaveChangesAsync();
            }
        }
    }
}
