using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Models;
using Sales_Management.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Sales_Management.Services;

namespace Sales_Management.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly SalesManagementContext _context;
        private readonly ICoinService _coinService;

        public OrdersController(SalesManagementContext context, ICoinService coinService)
        {
            _context = context;
            _coinService = coinService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(int productId, int quantity = 1)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Lấy sản phẩm để xử lý theo logic Entity Framework
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null || userIdString == null) return NotFound();

            // 1. Kiểm tra tồn kho trước
            if (product.StockQuantity < quantity)
            {
                TempData["Error"] = "Sản phẩm không đủ số lượng trong kho!";
                return RedirectToAction("Index", "Home");
            }

            // 2. Tính xu và trừ tiền qua Service
            // Sử dụng hàm CalculateCoin từ ICoinService để đồng bộ logic tỉ giá
            decimal priceInCoins = _coinService.CalculateCoin(product.SellingPrice) * quantity;

            // Gọi logic Wallet: Trừ tiền thành công mới thực hiện các bước tiếp theo
            var success = await _coinService.UseCoins(userIdString, priceInCoins);
            if (!success)
            {
                TempData["Error"] = "Giao dịch xu không thành công hoặc số dư không đủ!";
                return RedirectToAction("Index", "Home");
            }

            // 3. Thực hiện trừ kho và tạo đơn hàng (Logic Entity)
            try
            {
                // Trừ kho trực tiếp trên thực thể product
                product.StockQuantity -= quantity;
                _context.Entry(product).State = EntityState.Modified;

                // Tạo Đơn hàng
                var order = new Order
                {
                    CustomerId = int.Parse(userIdString),
                    OrderDate = DateTime.Now,
                    TotalAmount = product.SellingPrice * quantity,
                    Status = "Completed",
                    CreatedBy = int.Parse(userIdString)
                };
                _context.Orders.Add(order);

                // Lưu lần 1 để lấy OrderId
                await _context.SaveChangesAsync();

                // 4. Tạo OrderDetail
                var detail = new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = productId,
                    Quantity = quantity,
                    UnitPrice = product.SellingPrice,
                    Total = product.SellingPrice * quantity
                };
                _context.OrderDetails.Add(detail);

                // 5. Ghi nhật ký kho
                var inventory = new InventoryTransaction
                {
                    ProductId = productId,
                    Quantity = -quantity,
                    CreatedDate = DateTime.Now,
                    Type = "Sale",
                    CreatedBy = int.Parse(userIdString)
                };
                _context.InventoryTransactions.Add(inventory);

                // Lưu tất cả các thay đổi còn lại
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Mua thành công! Kho của {product.Name} đã được cập nhật.";
            }
            catch (Exception ex)
            {
                // Trong thực tế, nếu lỗi ở đây bạn nên có thêm hàm hoàn tiền (Refund) xu
                TempData["Error"] = "Lỗi khi cập nhật dữ liệu: " + ex.Message;
            }

            return RedirectToAction("Index", "Home");
        }

        // --- CÁC HÀM QUẢN LÝ (Giữ nguyên không thay đổi) ---

        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Index(string searchString, string statusFilter)
        {
            var orders = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.CreatedByNavigation)
                .OrderByDescending(o => o.OrderDate)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                orders = orders.Where(o => o.OrderId.ToString().Contains(searchString) ||
                                           o.Customer.FullName.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                orders = orders.Where(o => o.Status == statusFilter);
            }

            return View(await orders.ToListAsync());
        }

        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.CreatedByNavigation)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (order == null) return NotFound();

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.Status = status;
            _context.Update(order);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = id });
        }
    }
}