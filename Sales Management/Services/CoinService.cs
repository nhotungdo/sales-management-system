using Microsoft.EntityFrameworkCore;
using Sales_Management.Data;
using Sales_Management.Models;

namespace Sales_Management.Services
{
    public class CoinService : ICoinService
    {
        private readonly SalesManagementContext _context;

        public CoinService(SalesManagementContext context)
        {
            _context = context;
        }

        public decimal CalculateCoin(decimal price)
        {
            return price / 10;
        }

        public async Task<bool> UseCoins(string userId, decimal amount)
        {
            var customer = await _context.Customers
                .Include(c => c.Wallet)
                .FirstOrDefaultAsync(c => c.UserId == int.Parse(userId));

            if (customer?.Wallet == null || customer.Wallet.Balance < amount)
            {
                return false;
            }

            customer.Wallet.Balance -= amount;
            customer.Wallet.UpdatedDate = DateTime.Now;

            var transaction = new WalletTransaction
            {
                WalletId = customer.Wallet.WalletId,
                Amount = amount,
                TransactionType = "Payment",
                Method = "System",
                Status = "Completed",
                TransactionCode = $"PAY{DateTime.Now:yyMMddHHmmss}",
                Description = $"Thanh toán đơn hàng: -{amount} xu",
                CreatedDate = DateTime.Now
            };

            _context.WalletTransactions.Add(transaction);

            await _context.SaveChangesAsync();
            return true;
        }
    }
}