using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sales_Management.Data;
using Sales_Management.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

using Sales_Management.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Sales_Management.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly SalesManagementContext _context;
        private readonly IHubContext<SystemHub> _hubContext;
        private readonly ILogger<UsersController> _logger;

        public UsersController(SalesManagementContext context, IHubContext<SystemHub> hubContext, ILogger<UsersController> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string search, string role, string sortOrder)
        {
            ViewBag.CurrentSort = sortOrder;
            ViewBag.NameSortParm = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.DateSortParm = sortOrder == "Date" ? "date_desc" : "Date";
            ViewBag.CurrentFilter = search;
            ViewBag.CurrentRole = role;

            var users = _context.Users.AsQueryable();

            // Lọc bỏ người dùng đã xóa mềm? Hoặc hiển thị họ? 
            // Thường "Xóa" nghĩa là xóa mềm, nên ta có thể ẩn họ đi hoặc hiển thị với trạng thái.
            // Prompt ngụ ý chức năng "Xóa" thường ẩn khỏi danh sách.
            users = users.Where(u => !u.IsDeleted);

            if (!string.IsNullOrEmpty(search))
            {
                users = users.Where(u => u.Username.Contains(search) || u.Email.Contains(search) || (u.FullName != null && u.FullName.Contains(search)));
            }

            if (!string.IsNullOrEmpty(role))
            {
                users = users.Where(u => u.Role == role);
            }

            switch (sortOrder)
            {
                case "name_desc":
                    users = users.OrderByDescending(u => u.FullName ?? u.Username);
                    break;
                case "Date":
                    users = users.OrderBy(u => u.CreatedDate);
                    break;
                case "date_desc":
                    users = users.OrderByDescending(u => u.CreatedDate);
                    break;
                default:
                    users = users.OrderBy(u => u.FullName ?? u.Username);
                    break;
            }

            return View(await users.ToListAsync());
        }


        // GET: Admin/Users/Create (Form tạo người dùng mới)
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Users/Create (Xử lý tạo user)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FullName,Email,Role,IsActive")] User user, string Password)
        {
             // Sửa lỗi validation cho các trường không có trong form
            ModelState.Remove("Username");
            ModelState.Remove("PasswordHash");

            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    if (await _context.Users.AnyAsync(u => u.Email == user.Email))
                    {
                        ModelState.AddModelError("Email", "Email already exists.");
                        return View(user);
                    }

                    user.Username = user.Email; // Mặc định Username là Email
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password);
                    user.CreatedDate = DateTime.Now;
                    user.UpdatedDate = DateTime.Now;
                    
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    // Đồng bộ tự động: Tạo Employee nếu Role là Sales
                    if (user.Role == "Sales")
                    {
                        var employee = new Employee
                        {
                            UserId = user.UserId,
                            IsDeleted = false
                            // Other fields null
                        };
                        _context.Employees.Add(employee);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation($"Synchronized creation: Employee record created for User {user.UserId} (Sales).");
                    }

                    await transaction.CommitAsync();
                    await _hubContext.Clients.All.SendAsync("ReceiveUpdate", "ReloadData");

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error creating user.");
                    ModelState.AddModelError("", "Error creating user.");
                }
            }
            return View(user);
        }

        // GET: Admin/Users/Edit/5 (Form chỉnh sửa user)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

        // POST: Admin/Users/Edit/5 (Lưu thay đổi user)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UserId,Username,PasswordHash,Email,FullName,PhoneNumber,Role,IsActive,CreatedDate")] User user, string? NewPassword)
        {
            if (id != user.UserId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id);
                    if (existingUser == null) return NotFound();

                    // Khôi phục các trường không muốn thay đổi do đặc thù binding form hoặc bị ẩn
                    user.UpdatedDate = DateTime.Now;
                    user.IsDeleted = existingUser.IsDeleted;
                    user.LastLogin = existingUser.LastLogin;
                    user.GoogleId = existingUser.GoogleId;
                    user.Avatar = existingUser.Avatar;

                    // Xử lý thay đổi mật khẩu
                    if (!string.IsNullOrEmpty(NewPassword))
                    {
                        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
                    }
                    else
                    {
                        user.PasswordHash = existingUser.PasswordHash;
                    }

                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.UserId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Admin/Users/Delete/5 (Xác nhận xóa)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null) return NotFound();

            return View(user);
        }

        // POST: Admin/Users/Delete/5 (Xóa mềm user và dữ liệu liên quan)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
                if (user != null)
                {
                    // Xóa mềm User
                    user.IsDeleted = true;
                    user.IsActive = false; // Hủy kích hoạt
                    user.UpdatedDate = DateTime.Now;
                    
                    _context.Users.Update(user);

                    // Yêu cầu: Đồng bộ xóa cho role "Sales"
                    if (!string.IsNullOrEmpty(user.Role) && user.Role.Equals("Sales", StringComparison.OrdinalIgnoreCase))
                    {
                        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == id);
                        if (employee != null)
                        {
                            employee.IsDeleted = true;
                            _context.Employees.Update(employee);
                            _logger.LogInformation($"Synchronized deletion: Employee record for User {user.UserId} (Sales) marked as deleted.");
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation($"User {user.UserId} deleted successfully.");
                    
                    // Thông báo Real-time
                    await _hubContext.Clients.All.SendAsync("ReceiveUpdate", "ReloadData");
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting user.");
                ModelState.AddModelError("", "An error occurred while deleting the user.");
                return View("Delete", await _context.Users.FindAsync(id));
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }
}
