using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sales_Management.Data;
using Sales_Management.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Sales_Management.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly SalesManagementContext _context;

        public UsersController(SalesManagementContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string search, string role, string sortOrder)
        {
            ViewBag.CurrentSort = sortOrder;
            ViewBag.NameSortParm = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.DateSortParm = sortOrder == "Date" ? "date_desc" : "Date";
            ViewBag.CurrentFilter = search;
            ViewBag.CurrentRole = role;

            var users = _context.Users.AsQueryable();

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
    }
}
