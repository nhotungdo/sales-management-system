using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Data;
using Sales_Management.Models;
using Microsoft.AspNetCore.Authorization;
using Sales_Management.Areas.Sale.Models;

namespace Sales_Management.Areas.Sale.Controllers
{
    [Area("Sale")]
    [Authorize(Roles = "Sales, Admin")]
    public class ProductController : Controller
    {
        private readonly SalesManagementContext _context;

        public ProductController(SalesManagementContext context)
        {
            _context = context;
        }

        // GET: Sales/Product
        public async Task<IActionResult> Index(string searchString, string currentFilter, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString;

            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            var products = _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.Name.Contains(searchString) || p.Code.Contains(searchString));
            }

            int pageSize = 10;
            return View(await PaginatedList<Product>.CreateAsync(products.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // GET: Sales/Product/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name");
            return View();
        }

        // POST: Sales/Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Name,Description,CategoryId,ImportPrice,SellingPrice,StockQuantity,Status")] Product product, string ImageUrl)
        {
            if (ModelState.IsValid)
            {
                // Basic validation for duplicate code
                if (await _context.Products.AnyAsync(p => p.Code == product.Code))
                {
                    ModelState.AddModelError("Code", "Mã sản phẩm đã tồn tại.");
                    ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
                    return View(product);
                }

                _context.Add(product);
                await _context.SaveChangesAsync();

                // Handle Image (Simple URL for now as per schema)
                if (!string.IsNullOrEmpty(ImageUrl))
                {
                    var productImage = new ProductImage
                    {
                        ProductId = product.ProductId,
                        ImageUrl = ImageUrl,
                        IsPrimary = true
                    };
                    _context.ProductImages.Add(productImage);
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
            return View(product);
        }

        // GET: Sales/Product/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(m => m.ProductId == id);
                
            if (product == null)
            {
                return NotFound();
            }
            
            ViewData["ImageUrl"] = product.ProductImages.FirstOrDefault(p => p.IsPrimary ?? false)?.ImageUrl;
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
            return View(product);
        }

        // POST: Sales/Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductId,Code,Name,Description,CategoryId,ImportPrice,SellingPrice,StockQuantity,Status,CreatedDate,CreatedBy")] Product product, string ImageUrl)
        {
            if (id != product.ProductId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    
                    // Update main image if changed/added
                    if (!string.IsNullOrEmpty(ImageUrl))
                    {
                        var primaryImg = await _context.ProductImages.FirstOrDefaultAsync(p => p.ProductId == id && (p.IsPrimary ?? false));
                        if(primaryImg != null)
                        {
                            primaryImg.ImageUrl = ImageUrl;
                            _context.Update(primaryImg);
                        }
                        else
                        {
                            _context.ProductImages.Add(new ProductImage { ProductId = id, ImageUrl = ImageUrl, IsPrimary = true });
                        }
                    }
                    
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.ProductId))
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
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
            return View(product);
        }

        // GET: Sales/Product/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Sales/Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
    
}
