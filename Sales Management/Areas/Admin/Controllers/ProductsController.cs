using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Data;

[Area("Admin")]
public class ProductsController : Controller
{
    private readonly SalesManagementContext _context;

    public ProductsController(SalesManagementContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var products = await _context.Products
            .Include(p => p.Category)
            .ToListAsync();

        return View(products);
    }
}
