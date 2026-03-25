using Microsoft.AspNetCore.Mvc;
using SalesManagement.DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using SalesManagement.BLL.Interfaces;

namespace SalesManagement.Web.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class OrdersController : Controller
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        // GET: Orders
        public async Task<IActionResult> Index(string searchString, string statusFilter, int? pageNumber)
        {
            var orders = await _orderService.GetOrdersOverviewAsync(searchString, statusFilter);
            return View(orders);
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var order = await _orderService.GetOrderDetailsAsync(id.Value);
            if (order == null) return NotFound();

            return View(order);
        }

        // POST: Orders/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var success = await _orderService.UpdateOrderStatusAsync(id, status);
            if (!success) return NotFound();

            return RedirectToAction(nameof(Details), new { id = id });
        }
    }
}
