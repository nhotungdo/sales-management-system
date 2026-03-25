using Microsoft.AspNetCore.Mvc;
using SalesManagement.BLL.Interfaces;
using SalesManagement.DAL.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SalesManagement.Web.Areas.Sale.Controllers
{
    [Area("Sale")]
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

        public async Task<IActionResult> Index()
        {
            var orders = await _orderService.GetOrdersOverviewAsync(null, null);
            return View(orders);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Customers = await _customerService.GetAllCustomersAsync(null);
            var products = await _productService.GetAllProductsAsync();
            ViewBag.Products = products.Where(p => (p.StockQuantity ?? 0) > 0).ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int customerId, List<int> productIds, List<int> quantities)
        {
            if (productIds == null || quantities == null || productIds.Count != quantities.Count)
            {
                ModelState.AddModelError("", "Dữ liệu sản phẩm không hợp lệ");
                return RedirectToAction(nameof(Create));
            }

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
