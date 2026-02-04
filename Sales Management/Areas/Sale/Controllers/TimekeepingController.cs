using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Data;
using Sales_Management.Models;
using Sales_Management.Services;
using System.Security.Claims;

namespace Sales_Management.Areas.Sale.Controllers
{
    [Area("Sale")]
    [Route("api/sales/timekeeping")]
    [Authorize(Roles = "Sales")]
    public class TimekeepingController : Controller
    {
        private readonly SalesManagementContext _context;
        private readonly ITimeProvider _time;

        public TimekeepingController(SalesManagementContext context, ITimeProvider time)
        {
            _context = context;
            _time = time;
        }

        [HttpGet("my-history")]
        public async Task<IActionResult> GetMyHistory()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            
            if (employee == null) return NotFound("Employee profile not found");

            var history = await _context.TimeAttendances
                .Where(t => t.EmployeeId == employee.EmployeeId)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.CheckInTime)
                .Take(20)
                .Select(t => new 
                {
                    attendanceId = t.AttendanceId,
                    date = t.Date,
                    checkInTime = t.CheckInTime,
                    checkOutTime = t.CheckOutTime,
                    workHours = t.WorkHours,
                    status = t.Status,
                    notes = t.Notes,
                    minutesLate = t.MinutesLate,
                    deduction = t.DeductionAmount
                })
                .ToListAsync();

            return Ok(history);
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            
            if (employee == null) return NotFound("Employee profile not found");

            var today = _time.Today;
            var startOfMonth = new DateOnly(today.Year, today.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            var attendances = await _context.TimeAttendances
                .Where(t => t.EmployeeId == employee.EmployeeId && t.Date >= startOfMonth && t.Date <= endOfMonth)
                .ToListAsync();

            var totalDeduction = attendances.Sum(t => t.DeductionAmount);
            var lateCount = attendances.Count(t => t.MinutesLate > 0);
            
            // Check for latest session today
            var latestSession = attendances
                .Where(t => t.Date == today)
                .OrderByDescending(t => t.CheckInTime)
                .FirstOrDefault();

            bool isLateToday = latestSession != null && latestSession.MinutesLate > 0;
            string lateMessage = isLateToday ? $"Warning: You were {latestSession?.MinutesLate} minutes late today!" : "";

            return Ok(new 
            {
                totalDeduction,
                lateCount,
                isLateToday,
                lateMessage,
                minutesLateToday = latestSession?.MinutesLate ?? 0
            });
        }
    }
}
