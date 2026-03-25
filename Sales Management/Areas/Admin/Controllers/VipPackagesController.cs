using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManagement.BLL.Interfaces;
using SalesManagement.DAL.Entities;
using System.Threading.Tasks;

namespace SalesManagement.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class VipPackagesController : Controller
    {
        private readonly IVipPackageService _packageService;

        public VipPackagesController(IVipPackageService packageService)
        {
            _packageService = packageService;
        }

        // Lấy danh sách gói VIP
        public async Task<IActionResult> Index()
        {
            var packages = await _packageService.GetAllPackagesAsync();
            return View(packages);
        }

        // Form tạo gói VIP mới
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // Xử lý tạo gói VIP
        public async Task<IActionResult> Create(VipPackage vipPackage)
        {
            if (ModelState.IsValid)
            {
                var result = await _packageService.AddPackageAsync(vipPackage);
                if (result)
                {
                    return RedirectToAction(nameof(Index));
                }
            }
            return View(vipPackage);
        }

        // GET: Admin/VipPackages/Edit/5 (Form sửa gói VIP)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var vipPackage = await _packageService.GetPackageByIdAsync(id.Value);
            if (vipPackage == null) return NotFound();

            return View(vipPackage);
        }

        // POST: Admin/VipPackages/Edit/5 (Lưu thay đổi gói VIP)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, VipPackage vipPackage)
        {
            if (id != vipPackage.VipPackageId) return NotFound();

            if (ModelState.IsValid)
            {
                var result = await _packageService.UpdatePackageAsync(vipPackage);
                if (result)
                {
                    return RedirectToAction(nameof(Index));
                }
            }
            return View(vipPackage);
        }

        // GET: Admin/VipPackages/Delete/5 (Xác nhận xóa)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var vipPackage = await _packageService.GetPackageByIdAsync(id.Value);
            if (vipPackage == null) return NotFound();

            return View(vipPackage);
        }

        // POST: Admin/VipPackages/Delete/5 (Xóa gói VIP)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _packageService.DeletePackageAsync(id);
            if (result)
            {
                return RedirectToAction(nameof(Index));
            }
            return NotFound();
        }
    }
}
