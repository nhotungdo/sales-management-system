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

        // GET: Admin/Products (Danh sách sản phẩm, quản lý tìm kiếm, sắp xếp và phân trang)
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
            
            // Sử dụng ToListAsync tạm thời, trong thực tế nên dùng PagedList<T>
            // Phân trang thủ công đơn giản theo yêu cầu
            var count = await products.CountAsync();
            var items = await products.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            ViewBag.CurrentPage = pageNumber;
            // Pass search string and sort order to view for persistence
            ViewBag.SearchString = searchString;
            ViewBag.SortOrder = sortOrder;
            
            return View(items);
        }

        // GET: Admin/Products/Details/5 (Xem chi tiết sản phẩm)
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

        // GET: Admin/Products/Create (Form tạo sản phẩm mới)
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
                
                // Lấy ID người dùng hiện tại (giả định thiết lập User Identity chuẩn)
                // Nếu dùng auth tùy chỉnh, hãy điều chỉnh cho phù hợp.
                var userName = User.Identity?.Name;
                if (!string.IsNullOrEmpty(userName))
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == userName);
                    if (user != null) product.CreatedBy = user.UserId;
                }

                // Tính toán Giá Coin
                product.CoinPrice = _coinService.CalculateCoin(product.SellingPrice);

                _context.Add(product);
                await _context.SaveChangesAsync();

                // Xử lý Hình ảnh
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
                            
                            // Chỉ ảnh hợp lệ đầu tiên được đặt làm ảnh chính (Primary)
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

        // GET: Admin/Products/Edit/5 (Form chỉnh sửa sản phẩm)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
            return View(product);
        }

        // POST: Admin/Products/Edit/5 (Lưu thay đổi sản phẩm)
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
                    // Giữ nguyên thông tin người tạo nếu không được truyền vào đúng (logic cập nhật đơn giản)
                    
                    // Tính lại Giá Coin
                    product.CoinPrice = _coinService.CalculateCoin(product.SellingPrice);

                    _context.Update(product);
                    await _context.SaveChangesAsync();

                    if (imageFile != null && imageFile.Length > 0)
                    {
                        // Xử lý ảnh: Thêm ảnh mới làm ảnh chính, hạ cấp ảnh cũ
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

        // GET: Admin/Products/Delete/5 (Xác nhận xóa)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null) return NotFound();

            return View(product);
        }

        // POST: Admin/Products/Delete/5 (Xử lý xóa mềm)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                // Lưu ý: Sử dụng Status = "Deleted" để xóa mềm theo yêu cầu
                
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
