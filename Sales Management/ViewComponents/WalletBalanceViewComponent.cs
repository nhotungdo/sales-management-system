using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesManagement.DAL.Data;
using System.Security.Claims;

namespace SalesManagement.Web.ViewComponents
{
    public class WalletBalanceViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;

        public WalletBalanceViewComponent(AppDbContext context)
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
