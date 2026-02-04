using Microsoft.EntityFrameworkCore;
using Sales_Management.Data;
using Sales_Management.Models;
// using BCrypt.Net;
using Microsoft.AspNetCore.SignalR;
using Sales_Management.Hubs;

namespace Sales_Management.Services
{
    public class AuthService : IAuthService
    {
        private readonly SalesManagementContext _context;
        private readonly IHubContext<SystemHub> _hubContext;

        public AuthService(SalesManagementContext context, IHubContext<SystemHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }



        public async Task<string> GetSalesCheckInStatus(int userId)
        {
             var user = await _context.Users
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.UserId == userId);

             if (user == null || user.Role != "Sales") return "Error";
             

             if (user.Employee == null) return "None"; // No employee record, treated as fresh

             var today = DateOnly.FromDateTime(DateTime.Now);

             // Check active session
             var activeSession = await _context.TimeAttendances
                .OrderByDescending(t => t.AttendanceId)
                .FirstOrDefaultAsync(t => 
                    t.EmployeeId == user.Employee.EmployeeId && 
                    t.Date == today &&
                    t.CheckOutTime == null);

             if (activeSession != null) return activeSession.Status ?? "Present";

             // Check last Closed session today
             var lastSession = await _context.TimeAttendances
                .Where(t => t.EmployeeId == user.Employee.EmployeeId && t.Date == today)
                 .OrderByDescending(t => t.CheckOutTime)
                .FirstOrDefaultAsync();
             
             if (lastSession != null) return lastSession.Status ?? "checked-out";

             return "None";
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

            if (user == null || user.Role != "Sales") return;

            // Auto-create Employee if missing
            if (user.Employee == null)
            {
                var newEmp = new Employee
                {
                    UserId = user.UserId,
                    Position = "Sales Staff",
                    BasicSalary = 5000000,
                    ContractType = "FullTime",
                    StartWorkingDate = DateOnly.FromDateTime(DateTime.Now),
                    Department = "Sales",
                    IsDeleted = false
                };
                _context.Employees.Add(newEmp);
                await _context.SaveChangesAsync();

                // Manually attach if possible, or reload
                user.Employee = newEmp;
            }

            if (user.Employee == null) return;

            var today = DateOnly.FromDateTime(DateTime.Now);
            
            // Check if there is already an active session (not checked out)
            var activeSession = await _context.TimeAttendances
                .FirstOrDefaultAsync(t => 
                    t.EmployeeId == user.Employee.EmployeeId && 
                    t.Date == today && 
                    t.CheckOutTime == null);

            if (activeSession == null)
            {
                // Determine Shift
                int? shiftId = user.Employee.ShiftId;
                
                // If no assigned shift, try to find a matching one
                if (shiftId == null)
                {
                    // Fallback: Find shift that covers current time
                    var timeNow = DateTime.Now.TimeOfDay;
                    var matchedShift = await _context.Shifts
                        .FirstOrDefaultAsync(s => s.StartTime <= timeNow && s.EndTime >= timeNow);
                    shiftId = matchedShift?.ShiftId;
                }

                var attendance = new TimeAttendance
                {
                    EmployeeId = user.Employee.EmployeeId,
                    ShiftId = shiftId, 
                    Date = today,
                    CheckInTime = DateTime.Now,
                    Status = "Checked in",
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
                
                // Rule (2): Early logout verification
                Shift? shift = null;
                if (activeSession.ShiftId != null)
                {
                    shift = await _context.Shifts.FindAsync(activeSession.ShiftId);
                }
                
                bool isEarly = false;
                if (shift != null)
                {
                    var timeNow = DateTime.Now.TimeOfDay;
                    // If current time is strictly before end time
                    if (timeNow < shift.EndTime)
                    {
                        isEarly = true;
                    }
                }

                if (isEarly && string.IsNullOrWhiteSpace(reason))
                {
                    activeSession.Status = "Early shift end without reason";
                    activeSession.Notes = "Automatic detection: Early Logout";
                    
                    // Real-time notification
                    await _hubContext.Clients.All.SendAsync("ReceiveUpdate", 
                        $"ALERT: Employee {user.FullName} ({user.Username}) ended shift early at {DateTime.Now:HH:mm} without reason!");
                }
                else
                {
                    activeSession.Status = "CheckedOut";
                    activeSession.Notes = reason;
                }
                
                await _context.SaveChangesAsync();
            }
        }


        public async Task<List<TimeAttendance>> GetRecentAttendance(int userId, int count)
        {
            var user = await _context.Users
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null || user.Employee == null)
                return new List<TimeAttendance>();

            return await _context.TimeAttendances
                .Where(t => t.EmployeeId == user.Employee.EmployeeId)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.CheckInTime)
                .Take(count)
                .ToListAsync();
        }
    }
}
