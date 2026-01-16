using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Data;
using Sales_Management.Models;

namespace Sales_Management.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SettingsController : Controller
    {
        private readonly SalesManagementContext _context;

        public SettingsController(SalesManagementContext context)
        {
            _context = context;
        }

        // GET: Admin/Settings
        public async Task<IActionResult> Index()
        {
            var settings = await _context.SystemSettings.ToDictionaryAsync(s => s.SettingKey, s => s.SettingValue);
            
            // Seed default values if empty (view only)
            ViewData["StoreName"] = settings.ContainsKey("StoreName") ? settings["StoreName"] : "Fashion Store";
            ViewData["StoreEmail"] = settings.ContainsKey("StoreEmail") ? settings["StoreEmail"] : "contact@store.com";
            ViewData["StoreAddress"] = settings.ContainsKey("StoreAddress") ? settings["StoreAddress"] : "123 Shopping Street";
            ViewData["Currency"] = settings.ContainsKey("Currency") ? settings["Currency"] : "VND";
            ViewData["MaintenanceMode"] = settings.ContainsKey("MaintenanceMode") && settings["MaintenanceMode"] == "true";

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateGeneral(string StoreName, string StoreEmail, string StoreAddress, string Currency, bool MaintenanceMode)
        {
            await UpdateSetting("StoreName", StoreName, "General");
            await UpdateSetting("StoreEmail", StoreEmail, "General");
            await UpdateSetting("StoreAddress", StoreAddress, "General");
            await UpdateSetting("Currency", Currency, "General");
            await UpdateSetting("MaintenanceMode", MaintenanceMode.ToString().ToLower(), "System");

            TempData["SuccessMessage"] = "Cập nhật cài đặt thành công!";
            return RedirectToAction(nameof(Index));
        }

        private async Task UpdateSetting(string key, string value, string group)
        {
            var setting = await _context.SystemSettings.FindAsync(key);
            if (setting == null)
            {
                setting = new SystemSetting { SettingKey = key, SettingValue = value, GroupName = group };
                _context.SystemSettings.Add(setting);
            }
            else
            {
                setting.SettingValue = value;
                _context.SystemSettings.Update(setting);
            }
            await _context.SaveChangesAsync();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePassword(string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            if (NewPassword != ConfirmPassword)
            {
                TempData["ErrorMessage"] = "Mật khẩu mới không khớp.";
                return RedirectToAction(nameof(Index));
            }

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Login", "Account", new { area = "" });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return NotFound();

            bool isValid = false;
            try
            {
                isValid = BCrypt.Net.BCrypt.Verify(CurrentPassword, user.PasswordHash);
            }
            catch(BCrypt.Net.SaltParseException)
            {
                 // Fallback for plain text if any (legacy support)
                 if(user.PasswordHash == CurrentPassword) isValid = true;
            }

            if (!isValid)
            {
                TempData["ErrorMessage"] = "Mật khẩu hiện tại không đúng.";
                return RedirectToAction(nameof(Index));
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
