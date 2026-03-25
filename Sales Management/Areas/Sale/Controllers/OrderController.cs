using Microsoft.AspNetCore.Mvc;
using SalesManagement.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using SalesManagement.Web.Areas.Sale.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SalesManagement.Web.Areas.Sale.Controllers
{
    [Area("Sale")]
    [Authorize(Roles = "Sales, Admin")]
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        // GET: Sales/Order
        public async Task<IActionResult> Index(string status, string searchString, int? pageNumber)
        {
            var orders = await _orderService.GetOrdersOverviewAsync(searchString, status);
            ViewData["CurrentStatus"] = status;
            ViewData["CurrentFilter"] = searchString;

            int pageSize = 10;
            return View(PaginatedList<SalesManagement.DAL.Entities.Order>.Create(orders.AsQueryable(), pageNumber ?? 1, pageSize));
        }

        // GET: Sales/Order/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var order = await _orderService.GetOrderDetailsAsync(id.Value);
            if (order == null) return NotFound();

            return View(order);
        }

        // POST: Sales/Order/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string newStatus)
        {
            var result = await _orderService.UpdateOrderStatusAsync(id, newStatus);
            if (!result) return NotFound();

            return RedirectToAction(nameof(Details), new { id = id });
        }
    }
}
