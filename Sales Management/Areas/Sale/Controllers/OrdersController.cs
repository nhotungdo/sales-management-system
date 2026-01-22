using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Data;
using Sales_Management.Models;

namespace Sales_Management.Areas.Sale.Controllers
{
    [Area("Sale")]
    public class OrdersController : Controller
    {
        private readonly SalesManagementContext _context;

        public OrdersController(SalesManagementContext context)
        {
            _context = context;
        }

        // LIST ORDER
        public async Task<IActionResult> Index()
        {
            var orders = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails);

            return View(await orders.ToListAsync());
        }
        public IActionResult Create()
        {
            ViewBag.Customers = _context.Customers.ToList();
            ViewBag.Products = _context.Products
                .Where(p => p.StockQuantity > 0)
                .ToList();

            return View();
        }
                [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int customerId,
            List<int> productIds,
            List<int> quantities
        )
        {
            if (productIds.Count != quantities.Count)
            {
                ModelState.AddModelError("", "Dữ liệu sản phẩm không hợp lệ");
            }

            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Create));
            }

            var order = new Order
            {
                CustomerId = customerId,
                OrderDate = DateTime.Now,
                Status = "Pending",
                PaymentStatus = "Unpaid",
                CreatedBy = 1 // TODO: lấy từ User đăng nhập
            };

            decimal subTotal = 0;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(); // lấy OrderId

            for (int i = 0; i < productIds.Count; i++)
            {
                var product = await _context.Products.FindAsync(productIds[i]);

                if (product == null) continue;

                if (quantities[i] <= 0 || quantities[i] > product.StockQuantity)
                {
                    continue; // không cho bán sai
                }

                var detail = new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = product.ProductId,
                    Quantity = quantities[i],
                    UnitPrice = product.SellingPrice,
                    Total = quantities[i] * (product.SellingPrice)
                };

                subTotal += detail.Total ?? 0;

                product.StockQuantity -= quantities[i]; // trừ kho

                _context.OrderDetails.Add(detail);
            }

            order.SubTotal = subTotal;
            order.TaxAmount = subTotal * 0.1m;
            order.TotalAmount = order.SubTotal + order.TaxAmount;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}


