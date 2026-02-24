using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Data;
using Sales_Management.Models;
using Sales_Management.ViewModels;
using Sales_Management.Services;
using System.Security.Claims;

namespace Sales_Management.Controllers
{
    public class CartController : Controller
    {
        private readonly SalesManagementContext _context;
        private readonly ICoinService _coinService;

        public CartController(SalesManagementContext context, ICoinService coinService)
        {
            _context = context;
            _coinService = coinService;
        }

        // 1. TRANG GIỎ HÀNG
        public async Task<IActionResult> Index()
        {
            // Cố gắng kích hoạt Session nếu nó chưa được khởi tạo
            HttpContext.Session.SetString("_KeepAlive", "1");

            var sessionId = HttpContext.Session.Id;

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.SessionId == sessionId)
                .Select(c => new CartItemViewModel
                {
                    ProductId = c.ProductId,
                    Name = c.Product.Name,
                    Price = c.Product.SellingPrice / 10, // Chia 10 như logic trang Detail của bạn
                    Quantity = c.Quantity,
                    ImageUrl = _context.ProductImages
                                .Where(i => i.ProductId == c.ProductId && i.IsPrimary == true)
                                .Select(i => i.ImageUrl).FirstOrDefault() ?? "/images/no-image.png"
                }).ToListAsync();

            return View(cartItems);
        }

        // 2. THÊM VÀO GIỎ
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity)
        {
            // Quan trọng: Lưu một giá trị bất kỳ vào Session để cố định Session ID
            HttpContext.Session.SetString("_KeepAlive", "1");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null) return Json(new { success = false, message = "Sản phẩm không tồn tại" });

                if (product.StockQuantity < quantity)
                    return Json(new { success = false, message = $"Số lượng tồn kho không đủ (Còn: {product.StockQuantity})" });

                // BƯỚC A: Trừ kho ngay lập tức
                product.StockQuantity -= quantity;
                _context.Update(product);

                // BƯỚC B: Lấy SessionId sau khi đã "mồi" ở trên
                var sessionId = HttpContext.Session.Id;

                var existingItem = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.SessionId == sessionId && c.ProductId == productId);

                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                    existingItem.ExpiryTime = DateTime.Now.AddMinutes(30);
                    _context.Update(existingItem);
                }
                else
                {
                    var newItem = new CartItem
                    {
                        SessionId = sessionId,
                        ProductId = productId,
                        Quantity = quantity,
                        ExpiryTime = DateTime.Now.AddMinutes(30)
                    };
                    _context.CartItems.Add(newItem);
                }

                // BƯỚC C: Ghi nhật ký
                _context.InventoryTransactions.Add(new InventoryTransaction
                {
                    ProductId = productId,
                    Quantity = -quantity,
                    Type = "Hold-In-Cart",
                    CreatedDate = DateTime.Now,
                    CreatedBy = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0")
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Đã giữ hàng trong giỏ của bạn (30 phút)!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Lỗi xử lý: " + ex.Message });
            }
        }

        // 3. XÓA KHỎI GIỎ: Hoàn lại kho ngay lập tức
        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            var sessionId = HttpContext.Session.Id;
            var item = await _context.CartItems.FirstOrDefaultAsync(c => c.SessionId == sessionId && c.ProductId == productId);

            if (item != null)
            {
                var product = await _context.Products.FindAsync(productId);
                if (product != null)
                {
                    product.StockQuantity += item.Quantity; // Hoàn kho
                    _context.Update(product);

                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        ProductId = productId,
                        Quantity = item.Quantity,
                        Type = "Release-From-Cart",
                        CreatedDate = DateTime.Now
                    });
                }
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true });
        }

        // 4. THANH TOÁN TOÀN BỘ GIỎ HÀNG: Trừ xu và chốt đơn
        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            var sessionId = HttpContext.Session.Id;
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString)) return Json(new { success = false, message = "Vui lòng đăng nhập!" });

            var cartItems = await _context.CartItems.Include(c => c.Product).Where(c => c.SessionId == sessionId).ToListAsync();
            if (!cartItems.Any()) return Json(new { success = false, message = "Giỏ hàng trống!" });

            // Tính tổng xu (Dựa trên logic coinService bạn đã dùng ở OrdersController)
            decimal totalCoins = 0;
            foreach (var item in cartItems)
            {
                totalCoins += _coinService.CalculateCoin(item.Product.SellingPrice) * item.Quantity;
            }

            // A. Trừ xu từ ví
            var paySuccess = await _coinService.UseCoins(userIdString, totalCoins);
            if (!paySuccess) return Json(new { success = false, message = "Số dư xu không đủ!" });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // B. Tạo Order
                var order = new Order
                {
                    CustomerId = int.Parse(userIdString),
                    OrderDate = DateTime.Now,
                    TotalAmount = cartItems.Sum(c => c.Product.SellingPrice * c.Quantity),
                    Status = "Completed",
                    CreatedBy = int.Parse(userIdString)
                };
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // C. Tạo OrderDetails & Xóa CartItems
                foreach (var item in cartItems)
                {
                    _context.OrderDetails.Add(new OrderDetail
                    {
                        OrderId = order.OrderId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Product.SellingPrice,
                        Total = item.Product.SellingPrice * item.Quantity
                    });
                }

                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Thanh toán thành công!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Lỗi thanh toán: " + ex.Message });
            }
        }
    }
}