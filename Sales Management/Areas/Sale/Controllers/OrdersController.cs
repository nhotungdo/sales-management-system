using Microsoft.AspNetCore.Mvc;
using SalesManagement.BLL.Interfaces;
using SalesManagement.DAL.Entities;
using Microsoft.AspNetCore.Authorization;

namespace SalesManagement.Web.Areas.Sale.Controllers
{
    [Area("Sale")]
    [Authorize(Roles = "Sales, Admin")]
    public class OrdersController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ICustomerService _customerService;
        private readonly IProductService _productService;

        public OrdersController(IOrderService orderService, ICustomerService customerService, IProductService productService)
        {
            _orderService = orderService;
            _customerService = customerService;
            _productService = productService;
        }

        // GET: Sale/Orders
        public async Task<IActionResult> Index(string searchString, string statusFilter)
        {
            var orders = await _orderService.GetOrdersOverviewAsync(searchString, statusFilter);
            ViewData["CurrentFilter"] = searchString;
            ViewData["StatusFilter"] = statusFilter;
            return View(orders);
        }

        // GET: Sale/Orders/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Customers = await _customerService.GetAllCustomersAsync(null);
            var products = await _productService.GetPagedProductsAsync(1, 100, null, null);
            ViewBag.Products = products.Where(p => (p.StockQuantity ?? 0) > 0).ToList();
            return View();
        }

        // POST: Sale/Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int customerId, List<int> productIds, List<int> quantities)
        {
            if (productIds == null || quantities == null || productIds.Count != quantities.Count)
            {
                ModelState.AddModelError("", "Dữ liệu sản phẩm không hợp lệ");
                return RedirectToAction(nameof(Create));
            }

            // Gọi logic tạo đơn hàng transactional từ Service
            var order = await _orderService.CreateOrderAsync(customerId, productIds, quantities);
            if (order == null)
            {
                ModelState.AddModelError("", "Lỗi trong quá trình tạo đơn hàng.");
                return RedirectToAction(nameof(Create));
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
