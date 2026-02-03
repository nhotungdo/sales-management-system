using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Data;
using Sales_Management.Models;

using Sales_Management.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Sales_Management.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class EmployeesController : Controller
    {
        private readonly SalesManagementContext _context;
        private readonly IHubContext<SystemHub> _hubContext;
        private readonly ILogger<EmployeesController> _logger;

        public EmployeesController(SalesManagementContext context, IHubContext<SystemHub> hubContext, ILogger<EmployeesController> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

        // GET: Admin/Employees (Danh sách nhân viên, hỗ trợ tìm kiếm và lọc)
        public async Task<IActionResult> Index(string searchString, string contractType)
        {
            var employees = _context.Employees
                .Include(e => e.User)
                .Include(e => e.TimeAttendances)
                .Where(e => !e.IsDeleted);

            if (!string.IsNullOrEmpty(searchString))
            {
                employees = employees.Where(e =>
                    (e.User.FullName != null && e.User.FullName.Contains(searchString)) ||
                    (e.Position != null && e.Position.Contains(searchString)));
            }
            
            if (!string.IsNullOrEmpty(contractType))
            {
                employees = employees.Where(e => e.ContractType == contractType);
            }

            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentContractType"] = contractType;
            return View(await employees.ToListAsync());
        }

        // GET: Admin/Employees/Details/5 (Xem chi tiết nhân viên)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees
                .Include(e => e.User)
                .Include(e => e.TimeAttendances)
                .FirstOrDefaultAsync(m => m.EmployeeId == id);

            if (employee == null) return NotFound();

            return View(employee);
        }

        // GET: Admin/Employees/Create (Form tạo nhân viên mới)
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Employees/Create (Xử lý tạo nhân viên)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Position,BasicSalary,StartWorkingDate,Department,ContractType")] Employee employee, string FullName, string Email, string Password, string Role)
        {
            // Loại bỏ validation cho User vì chưa được bind

            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try 
                {
                    // Kiểm tra tồn tại user
                    if (await _context.Users.AnyAsync(u => u.Email == Email || u.Username == Email))
                    {
                        ModelState.AddModelError("Email", "Email/Username đã tồn tại trong hệ thống.");
                        ViewBag.FullName = FullName;
                        ViewBag.Email = Email;
                        ViewBag.Role = Role;
                        return View(employee);
                    }

                    var user = new User
                    {
                        FullName = FullName,
                        Email = Email,
                        Username = Email, 
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
                        Role = Role,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now, // Quan trọng cho SQL Server
                        IsActive = true
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    employee.UserId = user.UserId;
                    _context.Employees.Add(employee);
                    await _context.SaveChangesAsync();
                    
                    await transaction.CommitAsync();
                    
                    await _hubContext.Clients.All.SendAsync("ReceiveUpdate", "ReloadData");
                    
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error creating employee.");
                    ModelState.AddModelError("", "Error creating employee: " + ex.Message);
                }
            }
            ViewBag.FullName = FullName;
            ViewBag.Email = Email;
            ViewBag.Role = Role;
            return View(employee);
        }

        // GET: Admin/Employees/Edit/5 (Form chỉnh sửa nhân viên)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);
                
            if (employee == null) return NotFound();
            return View(employee);
        }

        // POST: Admin/Employees/Edit/5 (Cập nhật nhân viên)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("EmployeeId,UserId,Position,BasicSalary,StartWorkingDate,Department,ContractType")] Employee employee, string PhoneNumber)
        {
            if (id != employee.EmployeeId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(employee);
                    if(!string.IsNullOrEmpty(PhoneNumber))
                    {
                        var user = await _context.Users.FindAsync(employee.UserId);
                        if(user != null)
                        {
                            user.PhoneNumber = PhoneNumber;
                            _context.Update(user);
                        }
                    }
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Employees.Any(e => e.EmployeeId == employee.EmployeeId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(employee);
        }

        // GET: Admin/Employees/Delete/5 (Xác nhận xóa)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(m => m.EmployeeId == id);
            if (employee == null) return NotFound();

            return View(employee);
        }

        // POST: Admin/Employees/Delete/5 (Xóa mềm nhân viên)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                employee.IsDeleted = true; // Xóa mềm
                _context.Employees.Update(employee);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
