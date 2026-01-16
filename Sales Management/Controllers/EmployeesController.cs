using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Models;
using Sales_Management.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;

namespace Sales_Management.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EmployeesController : Controller
    {
        private readonly SalesManagementContext _context;

        public EmployeesController(SalesManagementContext context)
        {
            _context = context;
        }

        // GET: Employees
        public async Task<IActionResult> Index(string searchString)
        {
            var employees = _context.Employees.Include(e => e.User).Where(e => !e.IsDeleted);

            if (!string.IsNullOrEmpty(searchString))
            {
                employees = employees.Where(e =>
                    (e.User.FullName != null && e.User.FullName.Contains(searchString)) ||
                    (e.Position != null && e.Position.Contains(searchString)));
            }

            return View(await employees.ToListAsync());
        }

        // GET: Employees/Details/5
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

        // GET: Employees/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Employees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Position,BasicSalary,StartWorkingDate,Department,ContractType")] Employee employee, string FullName, string Email, string Password, string Role)
        {
            // In a real scenario, handle User creation transactionally and hash password
            var user = new User
            {
                FullName = FullName,
                Email = Email,
                Username = Email, // Simple username
                PasswordHash = Password, // TODO: Hash this!
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

        // GET: Employees/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.EmployeeId == id);
            if (employee == null) return NotFound();

            return View(employee);
        }

        // POST: Employees/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("EmployeeId,UserId,Position,BasicSalary,StartWorkingDate,Department,ContractType,ContractFile")] Employee employee)
        {
            if (id != employee.EmployeeId) return NotFound();

            try
            {
                // Track changes logic here for history
                _context.Update(employee);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Employees.Any(e => e.EmployeeId == employee.EmployeeId)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Employees/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(m => m.EmployeeId == id);
            if (employee == null) return NotFound();

            return View(employee);
        }

        // POST: Employees/Delete/5
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

        // Payroll Generation (Stub)
        public IActionResult GeneratePayroll()
        {
            // Logic to calculate payroll based on TimeAttendances
            // For now, redirect with message
            TempData["Message"] = "Đã bắt đầu tạo bảng lương.";
            return RedirectToAction(nameof(Index));
        }
    }
}
