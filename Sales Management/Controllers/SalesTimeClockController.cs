using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Data;
using Sales_Management.Models;
using System.Security.Claims;

namespace Sales_Management.Controllers.Api
{
    [Route("api/sales/timekeeping")]
    [ApiController]
    public class SalesTimeClockController : ControllerBase
    {
        private readonly SalesManagementContext _context;

        public SalesTimeClockController(SalesManagementContext context)
        {
            _context = context;
        }

        // GET: api/sales/timekeeping/history/{employeeId}
        [HttpGet("history/{employeeId}")]
        public async Task<IActionResult> GetHistory(int employeeId)
        {
             var history = await _context.TimeAttendances
                .Where(t => t.EmployeeId == employeeId)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.CheckInTime)
                .Select(t => new {
                    t.AttendanceId,
                    t.Date,
                    t.CheckInTime,
                    t.CheckOutTime,
                    t.Status,
                    t.Notes,
                    Duration = t.CheckOutTime.HasValue && t.CheckInTime.HasValue 
                        ? (t.CheckOutTime.Value - t.CheckInTime.Value).ToString(@"hh\:mm\:ss") 
                        : null
                })
                .ToListAsync();

            return Ok(history);
        }
        
        // GET: api/sales/timekeeping/my-history
        [HttpGet("my-history")]
        public async Task<IActionResult> GetMyHistory()
        {
             var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
             if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized();
             
             var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
             if (employee == null) return NotFound("Employee profile not found");
             
             return await GetHistory(employee.EmployeeId);
        }
    }
}
