using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManagement.BLL.Interfaces;
using SalesManagement.DAL.Entities;
using System.Threading.Tasks;

namespace SalesManagement.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SettingsController : Controller
    {
        private readonly ISettingService _settingService;

        public SettingsController(ISettingService settingService)
        {
            _settingService = settingService;
        }

        // GET: Admin/Settings (Danh sách các thiết lập hệ thống)
        public async Task<IActionResult> Index()
        {
            var settings = await _settingService.GetAllSettingsAsync();
            return View(settings);
        }

        // POST: Admin/Settings/Update (Cập nhật thiết lập)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(SystemSetting setting)
        {
            if (ModelState.IsValid)
            {
                var result = await _settingService.UpdateSettingAsync(setting);
                if (result)
                {
                    TempData["Success"] = "Cập nhật thiết lập thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
