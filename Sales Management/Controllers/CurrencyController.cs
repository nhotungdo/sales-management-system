using Microsoft.AspNetCore.Mvc;
using Sales_Management.Services;
using System.Threading.Tasks;
using System;

namespace Sales_Management.Controllers
{
    public class CurrencyController : Controller
    {
        private readonly ICurrencyService _currencyService;

        public CurrencyController(ICurrencyService currencyService)
        {
            _currencyService = currencyService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Convert([FromBody] ConversionRequest request)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Simple validation before service call for immediate feedback if needed, but service has robust validation.
            // Service validation includes 0 checks? Request says "ensure only positive numbers". Service handles < 0.
            // Service handles max limit.

            try
            {
                var cents = _currencyService.ConvertVndToCents(request.VndAmount);
                await _currencyService.LogConversionAsync(request.VndAmount, cents, ipAddress, true, "Success");
                return Json(new { success = true, cents = cents });
            }
            catch (ArgumentException ex)
            {
                // We log the failed attempt
                await _currencyService.LogConversionAsync(request.VndAmount, 0, ipAddress, false, ex.Message);
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                await _currencyService.LogConversionAsync(request.VndAmount, 0, ipAddress, false, ex.Message);
                return Json(new { success = false, message = "An unexpected error occurred." });
            }
        }

        public class ConversionRequest
        {
            public decimal VndAmount { get; set; }
        }
    }
}
