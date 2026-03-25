using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManagement.Web.ViewModels;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SalesManagement.BLL.Interfaces;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace SalesManagement.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;

        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _authService.ValidateUser(model.Username, model.Password);

            if (user == null)
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng");
                return View(model);
            }

            // Tạo claims
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
                        : DateTimeOffset.UtcNow.AddHours(10)
                });

            // Check-in cho Sales
            if (user.Role == "Sales")
            {
                await _authService.CheckInSalesEmployee(user.UserId);
            }

            // Redirect theo role
            if (user.Role == "Admin")
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            else if (user.Role == "Sales")
                return RedirectToAction("Index", "Home", new { area = "Sale" });
            else
                return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

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

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout(string? reason = null)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                 if (User.IsInRole("Sales"))
                 {
                     await _authService.CheckOutSalesEmployee(userId, reason ?? "User Logged Out");
                 }
            }
            
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
             var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
             if (!int.TryParse(userIdStr, out int userId)) return RedirectToAction(nameof(Login));

             var user = await _authService.GetUserProfileAsync(userId);
             if (user == null) return NotFound();

             var customer = user.Customer;
             var wallet = customer?.Wallet;

             var model = new CustomerProfileViewModel
             {
                 FullName = user.FullName,
                 Email = user.Email,
                 PhoneNumber = user.PhoneNumber,
                 Address = customer?.Address,
                 Avatar = user.Avatar,
                 CreatedDate = user.CreatedDate,
                 CustomerLevel = customer?.CustomerLevel,
                 WalletBalance = wallet?.Balance ?? 0,
                 WalletStatus = wallet?.Status,
                 WalletUpdatedDate = wallet?.UpdatedDate,
                 Orders = customer?.Orders.OrderByDescending(o => o.OrderDate).Select(o => new OrderHistoryViewModel
                 {
                      OrderId = o.OrderId,
                      OrderDate = o.OrderDate ?? DateTime.Now,
                      TotalAmount = o.TotalAmount ?? 0,
                      Status = o.Status
                 }).ToList() ?? new()
             };

             return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string FullName, string Email, string PhoneNumber, string Address, IFormFile? AvatarFile)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToAction(nameof(Login));

            string? avatarUrl = null;
            if (AvatarFile != null && AvatarFile.Length > 0)
            {
                string uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/avatars");
                if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(AvatarFile.FileName);
                string filePath = Path.Combine(uploadDir, fileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await AvatarFile.CopyToAsync(fileStream);
                }
                avatarUrl = "/img/avatars/" + fileName;
            }

            var success = await _authService.UpdateUserProfileAsync(userId, FullName, Email, PhoneNumber, Address, avatarUrl);
            if (success)
            {
                 TempData["Success"] = "Cập nhật thông tin thành công!";
            }
            else
            {
                 TempData["Error"] = "Cập nhật thất bại.";
            }

            return RedirectToAction(nameof(Profile));
        }
    }
}
