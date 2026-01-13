using Microsoft.AspNetCore.Mvc;

namespace Sales_Management.Areas.Sale.Controllers
{
    [Area("Sale")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
