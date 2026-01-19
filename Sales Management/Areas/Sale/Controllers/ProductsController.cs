using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
            ViewBag.CategoryId = new SelectList(_context.Categories, "CategoryId", "Name");
            return View();
        }

        // POST: Sale/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            bool exists = await _context.Products.AnyAsync(p => p.Code == product.Code);

            if (exists)
            {
                ModelState.AddModelError("Code", "Product code already exists!");
            }
            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CategoryId = new SelectList(
                _context.Categories,
                "CategoryId",
                "Name",
                product.CategoryId
            );
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
        public async Task<IActionResult> Edit(
        int id,
        Product product,
        IFormFile? imageFile)
        {
            if (id != product.ProductId) return NotFound();

            var dbProduct = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (dbProduct == null) return NotFound();

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
                dbProduct.ProductImages.Clear();

                dbProduct.ProductImages.Add(new ProductImage
                {
                    ImageUrl = "/images/" + fileName
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
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
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
