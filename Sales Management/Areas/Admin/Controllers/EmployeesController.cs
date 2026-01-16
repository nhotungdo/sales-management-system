using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Data;
using Sales_Management.Models;

namespace Sales_Management.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class EmployeesController : Controller
    {
        private readonly SalesManagementContext _context;

        public EmployeesController(SalesManagementContext context)
        {
            _context = context;
        }

        // GET: Admin/Employees
        public async Task<IActionResult> Index(string searchString)
        {
            var employees = _context.Employees.Include(e => e.User).Where(e => !e.IsDeleted);

            if (!string.IsNullOrEmpty(searchString))
            {
                employees = employees.Where(e =>
                    (e.User.FullName != null && e.User.FullName.Contains(searchString)) ||
                    (e.Position != null && e.Position.Contains(searchString)));
            }
            
            ViewData["CurrentFilter"] = searchString;
            return View(await employees.ToListAsync());
        }

        // GET: Admin/Employees/Details/5
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

        // GET: Admin/Employees/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Employees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Position,BasicSalary,StartWorkingDate,Department,ContractType")] Employee employee, string FullName, string Email, string Password, string Role)
        {
             // Note: simplistic user creation for demo. Needs proper validation & hashing in production.
            if (ModelState.IsValid)
            {
                 var user = new User
                {
                    FullName = FullName,
                    Email = Email,
                    Username = Email, 
                    PasswordHash = Password, // TODO: Hash password
                    Role = Role,
                    CreatedDate = DateTime.Now,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                employee.UserId = user.UserId;
                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            return View(employee);
        }

        // GET: Admin/Employees/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);
                
            if (employee == null) return NotFound();
            return View(employee);
        }

        // POST: Admin/Employees/Edit/5
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

        // GET: Admin/Employees/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(m => m.EmployeeId == id);
            if (employee == null) return NotFound();

            return View(employee);
        }

        // POST: Admin/Employees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                employee.IsDeleted = true; // Soft Delete
                _context.Employees.Update(employee);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
