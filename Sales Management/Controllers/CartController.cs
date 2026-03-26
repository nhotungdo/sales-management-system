using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManagement.BLL.Interfaces;
using SalesManagement.Web.ViewModels;
using System.Security.Claims;
using System.Text.Json;

namespace SalesManagement.Web.Controllers
{
    public class CartController : Controller
    {
        private const string CartSessionKey = "ShoppingCart";
        private readonly IProductService _productService;
        private readonly IOrderService _orderService;

        public CartController(IProductService productService, IOrderService orderService)
        {
            _productService = productService;
            _orderService = orderService;
        }

        // ──────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────
        private List<CartItemViewModel> GetCart()
        {
            var json = HttpContext.Session.GetString(CartSessionKey);
            return string.IsNullOrEmpty(json)
                ? new List<CartItemViewModel>()
                : JsonSerializer.Deserialize<List<CartItemViewModel>>(json)!;
        }

        private void SaveCart(List<CartItemViewModel> cart)
        {
            HttpContext.Session.SetString(CartSessionKey, JsonSerializer.Serialize(cart));
        }

        // ──────────────────────────────────────────────
        // GET: /Cart/Index
        // ──────────────────────────────────────────────
        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        // ──────────────────────────────────────────────
        // POST: /Cart/AddToCart  (AJAX)
        // ──────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            // Chưa đăng nhập → trả JSON để client tự redirect
            if (!(User.Identity?.IsAuthenticated ?? false))
                return Json(new { success = false, requireLogin = true });

            if (quantity < 1) quantity = 1;

            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null)
                return Json(new { success = false, message = "Sản phẩm không tồn tại." });

            if (product.Status != "Active")
                return Json(new { success = false, message = "Sản phẩm không còn bán." });

            var cart = GetCart();
            var existing = cart.FirstOrDefault(c => c.ProductId == productId);

            if (existing != null)
            {
                var newQty = existing.Quantity + quantity;
                if (newQty > product.StockQuantity)
                    return Json(new { success = false, message = $"Chỉ còn {product.StockQuantity} sản phẩm trong kho." });
                existing.Quantity = newQty;
            }
            else
            {
                if (quantity > product.StockQuantity)
                    return Json(new { success = false, message = $"Chỉ còn {product.StockQuantity} sản phẩm trong kho." });

                var primaryImg = product.ProductImages?.FirstOrDefault(i => i.IsPrimary == true)?.ImageUrl
                              ?? product.ProductImages?.FirstOrDefault()?.ImageUrl
                              ?? "/images/no-image.png";

                cart.Add(new CartItemViewModel
                {
                    ProductId = product.ProductId,
                    Name = product.Name,
                    ImageUrl = primaryImg,
                    Price = product.CoinPrice ?? 0,
                    Quantity = quantity
                });
            }

            SaveCart(cart);
            int totalItems = cart.Sum(c => c.Quantity);
            return Json(new { success = true, message = "Đã thêm vào giỏ hàng!", cartCount = totalItems });
        }

        // ──────────────────────────────────────────────
        // POST: /Cart/Remove
        // ──────────────────────────────────────────────
        [HttpPost]
        [Authorize]
        public IActionResult Remove(int productId)
        {
            var cart = GetCart();
            cart.RemoveAll(c => c.ProductId == productId);
            SaveCart(cart);
            return RedirectToAction(nameof(Index));
        }

        // ──────────────────────────────────────────────
        // POST: /Cart/UpdateQuantity  (AJAX)
        // ──────────────────────────────────────────────
        [HttpPost]
        [Authorize]
        public IActionResult UpdateQuantity(int productId, int quantity)
        {
            if (quantity < 1)
                return Json(new { success = false, message = "Số lượng không hợp lệ." });

            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.ProductId == productId);
            if (item == null)
                return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ." });

            item.Quantity = quantity;
            SaveCart(cart);

            decimal newTotal = item.Price * item.Quantity;
            decimal cartTotal = cart.Sum(c => c.Price * c.Quantity);
            return Json(new { success = true, newItemTotal = newTotal.ToString("N0"), cartTotal = cartTotal.ToString("N0") });
        }

        // ──────────────────────────────────────────────
        // POST: /Cart/CheckoutAll  — mua tất cả trong giỏ
        // ──────────────────────────────────────────────
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckoutAll()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdString == null) return Unauthorized();
            int userId = int.Parse(userIdString);

            var cart = GetCart();
            if (!cart.Any())
            {
                TempData["Error"] = "Giỏ hàng trống. Vui lòng thêm sản phẩm trước khi thanh toán.";
                return RedirectToAction(nameof(Index));
            }

            var items = cart.Select(c => (c.ProductId, c.Quantity)).ToList();
            var result = await _orderService.CheckoutCartAsync(userId, items);

            if (result.Success)
            {
                // Xóa giỏ sau khi thanh toán thành công
                HttpContext.Session.Remove(CartSessionKey);
                TempData["Success"] = result.Message;
                return RedirectToAction("MyOrders", "Orders");
            }
            else
            {
                TempData["Error"] = result.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // ──────────────────────────────────────────────
        // GET: /Cart/Count  (AJAX — đếm số item)
        // ──────────────────────────────────────────────
        [HttpGet]
        [Authorize]
        public IActionResult Count()
        {
            var cart = GetCart();
            return Json(new { count = cart.Sum(c => c.Quantity) });
        }
    }
}
