using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Data;
using Sales_Management.Models;
using Sales_Management.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace Sales_Management.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminProductsController : Controller
    {
        private readonly SalesManagementContext _context;
        private readonly ICoinService _coinService;

        public AdminProductsController(SalesManagementContext context, ICoinService coinService)
        {
            _context = context;
            _coinService = coinService;
        }

        // GET: Admin/Products
        public async Task<IActionResult> Index(string searchString, int? page, string sortOrder)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["PriceSortParm"] = sortOrder == "Price" ? "price_desc" : "Price";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";
            ViewData["CurrentFilter"] = searchString;

            var products = _context.Products.Include(p => p.Category).Include(p => p.ProductImages).Where(p => p.Status != "Deleted").AsQueryable();

            if (!String.IsNullOrEmpty(searchString))
            {
                products = products.Where(s => s.Name.Contains(searchString) || s.Code.Contains(searchString));
            }

            switch (sortOrder)
            {
                case "name_desc":
                    products = products.OrderByDescending(s => s.Name);
                    break;
                case "Price":
                    products = products.OrderBy(s => s.SellingPrice);
                    break;
                case "price_desc":
                    products = products.OrderByDescending(s => s.SellingPrice);
                    break;
                case "Date":
                    products = products.OrderBy(s => s.CreatedDate);
                    break;
                case "date_desc":
                    products = products.OrderByDescending(s => s.CreatedDate);
                    break;
                default:
                    products = products.OrderByDescending(s => s.CreatedDate); // Default sort
                    break;
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            
            // ToListAsync for now, but in production should use PagedList<T>
            // Simple manual pagination for requirement
            var count = await products.CountAsync();
            var items = await products.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            ViewBag.CurrentPage = pageNumber;
            // Pass search string and sort order to view for persistence
            ViewBag.SearchString = searchString;
            ViewBag.SortOrder = sortOrder;
            
            return View(items);
        }

        // GET: Admin/Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.CreatedByNavigation)
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // GET: Admin/Products/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Name,Description,CategoryId,ImportPrice,SellingPrice,Vatrate,StockQuantity,Status")] Product product, List<IFormFile> imageFiles)
        {
            if (await _context.Products.AnyAsync(p => p.Code == product.Code))
            {
                ModelState.AddModelError("Code", "Product code already exists.");
            }

            if (ModelState.IsValid)
            {
                product.CreatedDate = DateTime.Now;
                product.UpdatedDate = DateTime.Now;
                
                // Get current user ID (assuming standard User Identity setup)
                // If using custom auth, adjust accordingly. Safe to leave null if not critical or fetch from Claims.
                var userName = User.Identity?.Name;
                if (!string.IsNullOrEmpty(userName))
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == userName);
                    if (user != null) product.CreatedBy = user.UserId;
                }

                // Calculate Coin Price
                product.CoinPrice = _coinService.CalculateCoin(product.SellingPrice);

                _context.Add(product);
                await _context.SaveChangesAsync();

                // Image Handling
                if (imageFiles != null && imageFiles.Count > 0)
                {
                    bool isFirst = true;
                    foreach (var file in imageFiles)
                    {
                        if (file.Length > 0)
                        {
                            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);
                            
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            var productImage = new ProductImage
                            {
                                ProductId = product.ProductId,
                                ImageUrl = "/images/" + fileName,
                                IsPrimary = isFirst, 
                                CreatedDate = DateTime.Now
                            };
                            _context.ProductImages.Add(productImage);
                            
                            // Only the first valid image is primary
                            isFirst = false; 
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Product created successfully!";
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
            return View(product);
        }

        // GET: Admin/Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
            return View(product);
        }

        // POST: Admin/Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductId,Code,Name,Description,CategoryId,ImportPrice,SellingPrice,Vatrate,StockQuantity,Status,CreatedBy,CreatedDate")] Product product, IFormFile? imageFile)
        {
            if (id != product.ProductId) return NotFound();

            if (await _context.Products.AnyAsync(p => p.Code == product.Code && p.ProductId != id))
            {
                ModelState.AddModelError("Code", "Product code already exists.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == id);
                    if (existingProduct == null) return NotFound();

                    product.UpdatedDate = DateTime.Now;
                    // Preserve original created info if not passed correctly (though Bind includes it, safe to double check logic if needed)
                    // Logic: simple update here.
                    
                    // Recalculate Coin Price
                    product.CoinPrice = _coinService.CalculateCoin(product.SellingPrice);

                    _context.Update(product);
                    await _context.SaveChangesAsync();

                    if (imageFile != null && imageFile.Length > 0)
                    {
                        // Remove old images? Or just add new one as primary?
                        // Simple approach: Add new as primary, maybe remove old primary status
                        var userImages = _context.ProductImages.Where(pi => pi.ProductId == id);
                        foreach(var img in userImages) { img.IsPrimary = false; } // Demote others (or delete, user requirement vague, keeping history is safer usually but let's just add new one)
                        
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(stream);
                        }

                        var productImage = new ProductImage
                        {
                            ProductId = product.ProductId,
                            ImageUrl = "/images/" + fileName,
                            IsPrimary = true,
                            CreatedDate = DateTime.Now
                        };
                        _context.ProductImages.Add(productImage);
                        await _context.SaveChangesAsync();
                    }
                    
                    TempData["SuccessMessage"] = "Product updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.ProductId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
            return View(product);
        }

        // GET: Admin/Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null) return NotFound();

            return View(product);
        }

        // POST: Admin/Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                // Soft delete or hard delete? Request said "Consider soft deletion". 
                // Currently database schema has 'Status'. Let's set Status to 'Deleted' or 'Inactive'.
                // If there is an 'IsDeleted' column, use that.
                // Checking Product model in Step 16: It has 'Status' string, no 'IsDeleted'.
                // So I will set Status = 'Deleted'.
                
                product.Status = "Deleted"; // Soft Delete
                product.UpdatedDate = DateTime.Now;
                _context.Update(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Product deleted successfully (Soft Delete).";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}
