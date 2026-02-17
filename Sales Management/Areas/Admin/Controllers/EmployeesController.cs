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
                searchString = searchString.ToLower();
                employees = employees.Where(e =>
                    (e.User.FullName != null && e.User.FullName.ToLower().Contains(searchString)) ||
                    (e.Position != null && e.Position.ToLower().Contains(searchString)));
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
            ModelState.Remove("User");
            
            if (ModelState.IsValid)
            {
                // Validate required fields explicitly if needed
                if (string.IsNullOrEmpty(FullName) || string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
                {
                    ModelState.AddModelError("", "Vui lòng điền đầy đủ thông tin tài khoản.");
                }
                else
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
                        employee.IsDeleted = false;
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
        public async Task<IActionResult> Edit(int id, [Bind("EmployeeId,Position,BasicSalary,StartWorkingDate,Department,ContractType")] Employee employeeInput, string FullName, string Email, string Role, string PhoneNumber)
        {
            if (id != employeeInput.EmployeeId) return NotFound();

            ModelState.Remove("User"); // Avoid validation errors on User property which is null in binding

            // Validate manually passed fields
            if (string.IsNullOrEmpty(FullName)) ModelState.AddModelError("FullName", "Họ tên không được để trống");
            if (string.IsNullOrEmpty(Email)) ModelState.AddModelError("Email", "Email không được để trống");

            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Fetch existing employee with tracking
                    var existingEmployee = await _context.Employees
                        .Include(e => e.User)
                        .FirstOrDefaultAsync(e => e.EmployeeId == id);

                    if (existingEmployee == null) return NotFound();

                    // Update Employee properties
                    existingEmployee.Position = employeeInput.Position;
                    existingEmployee.BasicSalary = employeeInput.BasicSalary;
                    existingEmployee.StartWorkingDate = employeeInput.StartWorkingDate;
                    existingEmployee.Department = employeeInput.Department;
                    existingEmployee.ContractType = employeeInput.ContractType;
                    
                    // Update User properties
                    if (existingEmployee.User != null)
                    {
                        existingEmployee.User.FullName = FullName;
                        existingEmployee.User.PhoneNumber = PhoneNumber;
                        existingEmployee.User.Role = Role;
                        existingEmployee.User.UpdatedDate = DateTime.Now;

                        // Check if email changed and is unique
                        if (existingEmployee.User.Email != Email)
                        {
                            if (await _context.Users.AnyAsync(u => u.Email == Email && u.UserId != existingEmployee.UserId))
                            {
                                ModelState.AddModelError("Email", "Email đã được sử dụng bởi người khác.");
                                transaction.Rollback();
                                return View(existingEmployee);
                            }
                            existingEmployee.User.Email = Email;
                            existingEmployee.User.Username = Email; 
                        }
                    }

                    _context.Employees.Update(existingEmployee); // Updates Modified state
                    await _context.SaveChangesAsync();
                    
                    await transaction.CommitAsync();
                    
                    await _hubContext.Clients.All.SendAsync("ReceiveUpdate", "ReloadData");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Employees.Any(e => e.EmployeeId == id)) return NotFound();
                    else throw;
                }
                catch (Exception ex)
                {
                     // transaction.RollbackAsync() not strictly needed if usings are correct but good practice for explicit handling
                     try { await transaction.RollbackAsync(); } catch {}
                    _logger.LogError(ex, "Error updating employee");
                    ModelState.AddModelError("", "Error updating employee: " + ex.Message);
                    // Re-fetch to return view with valid data if possible, or just return what we have (which might be partial)
                    return View(employeeInput); 
                }
                return RedirectToAction(nameof(Index));
            }
            return View(employeeInput); // Note: this might miss User data if validation fails first time. 
            // Ideally we reload User data here too if we want to show the form again properly.
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
            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee != null)
            {
                // Soft delete employee
                employee.IsDeleted = true;
                
                // Deactivate user
                if (employee.User != null)
                {
                    employee.User.IsActive = false;
                    employee.User.UpdatedDate = DateTime.Now; 
                }

                _context.Employees.Update(employee);
                await _context.SaveChangesAsync();

                await _hubContext.Clients.All.SendAsync("ReceiveUpdate", "UserDeleted");
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
