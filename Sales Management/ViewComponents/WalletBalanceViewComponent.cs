using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Data;
using System.Security.Claims;

namespace Sales_Management.ViewComponents
{
    public class WalletBalanceViewComponent : ViewComponent
    {
        private readonly SalesManagementContext _context;

        public WalletBalanceViewComponent(SalesManagementContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Content("");
            }

            var username = User.Identity.Name;
            var user = await _context.Users
                .Include(u => u.Customer)
                .ThenInclude(c => c.Wallet)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user?.Customer?.Wallet != null)
            {
                return View(user.Customer.Wallet.Balance ?? 0);
            }

            return Content("");
        }
    }
}
