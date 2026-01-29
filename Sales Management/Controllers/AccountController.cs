using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Data;
using Sales_Management.Services;
using Sales_Management.ViewModels;

namespace Sales_Management.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly SalesManagementContext _context;

        public AccountController(IAuthService authService, SalesManagementContext context)
        {
            _authService = authService;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _authService.ValidateUser(model.Username, model.Password);

            if (user == null)
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("FullName", user.FullName ?? "")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe
                        ? DateTimeOffset.UtcNow.AddDays(30)
                        : DateTimeOffset.UtcNow.AddHours(1)
                });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            if (user.Role == "Admin")
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            else if (user.Role == "Sales")
                return RedirectToAction("Index", "Home", new { area = "Sale" });
            else
                return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _authService.RegisterUser(
                model.Username,
                model.Email,
                model.Password,
                model.FullName,
                model.PhoneNumber
            );

            if (user == null)
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc email đã tồn tại");
                return View(model);
            }

            TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction(nameof(Login));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToAction(nameof(Login));

            var profile = await (from u in _context.Users
                                 join c in _context.Customers on u.UserId equals c.UserId
                                 join w in _context.Wallets on c.CustomerId equals w.CustomerId
                                 where u.UserId == userId
                                 select new CustomerProfileViewModel
                                 {
                                     FullName = u.FullName,
                                     Email = u.Email,
                                     PhoneNumber = u.PhoneNumber,
                                     Address = c.Address,
                                     Avatar = u.Avatar,
                                     CreatedDate = u.CreatedDate,
                                     CustomerLevel = c.CustomerLevel,
                                     WalletBalance = w.Balance ?? 0,
                                     WalletStatus = w.Status,
                                     WalletUpdatedDate = w.UpdatedDate,
                                     Transactions = _context.WalletTransactions
                                        .Where(t => t.WalletId == w.WalletId)
                                        .OrderByDescending(t => t.CreatedDate)
                                        .Select(t => new WalletTransactionViewModel
                                        {
                                            TransactionCode = t.TransactionCode,
                                            Amount = t.Amount,
                                            Type = t.TransactionType,
                                            Status = t.Status,
                                            CreatedDate = t.CreatedDate ?? DateTime.Now,
                                            Description = t.Description
                                        }).Take(10).ToList()
                                 }).FirstOrDefaultAsync();

            if (profile == null) return NotFound();

            return View(profile);
        }
    }
}