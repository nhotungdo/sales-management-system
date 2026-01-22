using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Data;
using Sales_Management.Models;

namespace Sales_Management.Areas.Sale.Controllers
{
    [Area("Sale")]
    public class ProductsController : Controller
    {
        private readonly SalesManagementContext _context;

        public ProductsController(SalesManagementContext context)
        {
            _context = context;
        }

        // GET: Sale/Products
        public async Task<IActionResult> Index()
        {
            var products = _context.Products
                                   .Include(p => p.Category);
            return View(await products.ToListAsync());
        }

        // GET: Sale/Products/Create
        public IActionResult Create()
        {
            ViewBag.CategoryId = _context.Categories
                .AsNoTracking()
                .Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.Name
                })
                .ToList();

            return View();
        }

        // POST: Sale/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            bool exists = await _context.Products.AnyAsync(p => p.Code == product.Code);
            if (exists)
            {
                ModelState.AddModelError("Code", "Mã sản phẩm đã tồn tại!");
            }
            if (product.SellingPrice <= 0)
            {
                ModelState.AddModelError("SellingPrice", "Giá bán phải lớn hơn 0!");
            }
            if (product.StockQuantity < 0)
            {
                ModelState.AddModelError("StockQuantity", "Số lượng sản phẩm không được âm!");
            }
            if (ModelState.IsValid)
            {
                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // xử lý ảnh
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadPath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot/images"
                    );

                    if (!Directory.Exists(uploadPath))
                        Directory.CreateDirectory(uploadPath);

                    var fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                    var filePath = Path.Combine(uploadPath, fileName);

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

                return RedirectToAction(nameof(Index));
            }

            ViewBag.CategoryId = _context.Categories
            .AsNoTracking()
            .Select(c => new SelectListItem
            {
            Value = c.CategoryId.ToString(),
            Text = c.Name
            })
            .ToList();
            return View(product);
        }


        // GET: Sale/Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound();

            ViewBag.CategoryId = new SelectList(
                _context.Categories,
                "CategoryId",
                "Name",
                product.CategoryId
            );

            return View(product);
        }


        // POST: Sale/Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, IFormFile? imageFile)
        {
            if (id != product.ProductId) return NotFound();

            var dbProduct = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (dbProduct == null) return NotFound();
            // check trùng code
            bool codeExists = await _context.Products
            .AnyAsync(p => p.Code == product.Code && p.ProductId != id);

            if (codeExists)
            {
                ModelState.AddModelError("Code", "Product code already exists!");
            }
            if (product.SellingPrice <= 0)
            {
                ModelState.AddModelError("SellingPrice", "Giá bán phải lớn hơn 0!");
            }
            if (product.StockQuantity < 0)
            {
                ModelState.AddModelError("StockQuantity", "Số lượng sản phẩm không được âm!");
            }
            if (!ModelState.IsValid)
            {
                ViewBag.CategoryId = new SelectList(
                    _context.Categories,
                    "CategoryId",
                    "Name",
                    product.CategoryId
                );
                return View(product);
            }
            // update field
            dbProduct.Code = product.Code;
            dbProduct.Name = product.Name;
            dbProduct.SellingPrice = product.SellingPrice;
            dbProduct.StockQuantity = product.StockQuantity;
            dbProduct.Description = product.Description;
            dbProduct.CategoryId = product.CategoryId;

            // xử lý ảnh
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/images"
                );

                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                var fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                // xóa ảnh cũ
                _context.ProductImages.RemoveRange(dbProduct.ProductImages);

                dbProduct.ProductImages.Add(new ProductImage
                {
                    ImageUrl = "/images/" + fileName,
                    IsPrimary = true,
                    CreatedDate = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        // GET: Sale/Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // POST: Sale/Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products
            .Include(p => p.ProductImages)
            .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product != null)
            {
                // xóa file ảnh vật lý
                foreach (var img in product.ProductImages)
                {
                    if (!string.IsNullOrEmpty(img.ImageUrl))
                    {
                        var path = Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot",
                            img.ImageUrl.TrimStart('/')
                        );
                        if (System.IO.File.Exists(path))
                        {
                            System.IO.File.Delete(path);
                        }
                    }
                }
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
        // GET: Sale/Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)   // ảnh
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound();

            return View(product);
        }

    }
}