using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Data;
using Sales_Management.Models;

namespace Sales_Management.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class VipPackagesController : Controller
    {
        private readonly SalesManagementContext _context;

        public VipPackagesController(SalesManagementContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var packages = await _context.VipPackages
                .OrderBy(p => p.Price)
                .ToListAsync();
            return View(packages);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VipPackage vipPackage)
        {
            if (ModelState.IsValid)
            {
                _context.Add(vipPackage);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(vipPackage);
        }

        // GET: Admin/VipPackages/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vipPackage = await _context.VipPackages.FindAsync(id);
            if (vipPackage == null)
            {
                return NotFound();
            }
            return View(vipPackage);
        }

        // POST: Admin/VipPackages/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, VipPackage vipPackage)
        {
            if (id != vipPackage.VipPackageId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(vipPackage);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VipPackageExists(vipPackage.VipPackageId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(vipPackage);
        }

        // GET: Admin/VipPackages/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vipPackage = await _context.VipPackages
                .FirstOrDefaultAsync(m => m.VipPackageId == id);
            if (vipPackage == null)
            {
                return NotFound();
            }

            return View(vipPackage);
        }

        // POST: Admin/VipPackages/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var vipPackage = await _context.VipPackages.FindAsync(id);
            if (vipPackage != null)
            {
                _context.VipPackages.Remove(vipPackage);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool VipPackageExists(int id)
        {
            return _context.VipPackages.Any(e => e.VipPackageId == id);
        }
    }
}
