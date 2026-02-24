using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Data;
using Sales_Management.Models;
using Microsoft.AspNetCore.Authorization;
using Sales_Management.Areas.Sale.Models;
using System.Security.Claims;

namespace Sales_Management.Areas.Sale.Controllers
{
    [Area("Sale")]
    [Authorize(Roles = "Sales, Admin")]
    public class ProductsController : Controller
    {
        private readonly SalesManagementContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductsController(SalesManagementContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ================= INDEX =================
        public async Task<IActionResult> Index(string searchString, string currentFilter, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString;

            if (searchString != null)
                pageNumber = 1;
            else
                searchString = currentFilter;

            var products = _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p =>
                    p.Name.Contains(searchString) ||
                    p.Code.Contains(searchString));
            }

            int pageSize = 10;
            return View(await PaginatedList<Product>
                .CreateAsync(products.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // ================= CREATE =================
        public IActionResult Create()
        {
            ViewData["CategoryId"] =
                new SelectList(_context.Categories, "CategoryId", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
        [Bind("Code,Name,Description,CategoryId,ImportPrice,SellingPrice,StockQuantity,Status")]
        Product product,
        IFormFile? ImageFile,
        string? ImageUrl)
        {
            product.CreatedDate = DateTime.Now;

            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null)
                return Unauthorized();

            product.CreatedBy = int.Parse(claim.Value);

            // ================= VALIDATE GIÁ =================
            if (product.ImportPrice.HasValue &&
                product.SellingPrice <= product.ImportPrice.Value)
            {
                ModelState.AddModelError("SellingPrice",
                    "Giá bán phải lớn hơn giá nhập.");
            }

            // ================= CHỌN 1 TRONG 2 ẢNH =================
            string finalImageUrl = "";

            if (ImageFile != null && ImageFile.Length > 0)
            {
                string uploadFolder = Path.Combine(_env.WebRootPath, "uploads/products");
                Directory.CreateDirectory(uploadFolder);

                string fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                string filePath = Path.Combine(uploadFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                finalImageUrl = "/uploads/products/" + fileName;
            }
            else if (!string.IsNullOrWhiteSpace(ImageUrl))
            {
                finalImageUrl = ImageUrl.Trim();
            }
            else
            {
                ModelState.AddModelError("",
                    "Vui lòng chọn 1 trong 2: Upload file hoặc nhập URL ảnh.");
            }

            // ================= CHECK MODEL =================
            if (!ModelState.IsValid)
            {
                ViewData["CategoryId"] =
                    new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);

                return View(product);
            }

            // ================= CHECK TRÙNG CODE =================
            if (await _context.Products.AnyAsync(p => p.Code == product.Code))
            {
                ModelState.AddModelError("Code", "Mã sản phẩm đã tồn tại.");

                ViewData["CategoryId"] =
                    new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);

                return View(product);
            }

            // ================= LƯU PRODUCT =================
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // ================= LƯU ẢNH (NOT NULL) =================
            _context.ProductImages.Add(new ProductImage
            {
                ProductId = product.ProductId,
                ImageUrl = finalImageUrl,   
                IsPrimary = true,
                CreatedDate = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }



        // ================= EDIT =================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound();

            ViewData["ImageUrl"] =
                product.ProductImages.FirstOrDefault(i => i.IsPrimary == true)?.ImageUrl;

            ViewData["CategoryId"] =
                new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
        int id,
        [Bind("ProductId,Code,Name,Description,CategoryId,ImportPrice,SellingPrice,StockQuantity,Status,CreatedDate,CreatedBy")]
        Product product,
        IFormFile? ImageFile,
        string? ImageUrl)
        {
            if (id != product.ProductId)
                return NotFound();

            // ===== VALIDATE GIÁ =====
            if (product.ImportPrice.HasValue &&
                product.SellingPrice <= product.ImportPrice.Value)
            {
                ModelState.AddModelError("SellingPrice",
                    "Giá bán phải lớn hơn giá nhập.");
            }

            var existingImage = await _context.ProductImages
                .FirstOrDefaultAsync(p => p.ProductId == id && p.IsPrimary == true);

            // ===== CHỌN 1 TRONG 2 =====
            string finalImageUrl = existingImage?.ImageUrl; // mặc định giữ ảnh cũ

            if (ImageFile != null && ImageFile.Length > 0)
            {
                // ưu tiên upload file
                string uploadFolder = Path.Combine(_env.WebRootPath, "uploads/products");
                Directory.CreateDirectory(uploadFolder);

                string fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                string filePath = Path.Combine(uploadFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                finalImageUrl = "/uploads/products/" + fileName;
            }
            else if (!string.IsNullOrWhiteSpace(ImageUrl))
            {
                finalImageUrl = ImageUrl.Trim();
            }
            else if (existingImage == null)
            {
                ModelState.AddModelError("",
                    "Vui lòng chọn 1 trong 2: Upload file hoặc nhập URL.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["CategoryId"] =
                    new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);

                return View(product);
            }

            try
            {
                _context.Update(product);

                if (existingImage != null)
                {
                    existingImage.ImageUrl = finalImageUrl;
                    _context.Update(existingImage);
                }
                else
                {
                    _context.ProductImages.Add(new ProductImage
                    {
                        ProductId = id,
                        ImageUrl = finalImageUrl, 
                        IsPrimary = true,
                        CreatedDate = DateTime.Now
                    });
                }

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(product.ProductId))
                    return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // ================= DELETE =================
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null) return NotFound();

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
                _context.Products.Remove(product);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound();

            return View(product);
        }
    }
}

