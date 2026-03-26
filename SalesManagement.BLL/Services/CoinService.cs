using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SalesManagement.BLL.Interfaces;
using SalesManagement.DAL.Data;
using SalesManagement.DAL.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SalesManagement.BLL.Services
{
    public class CoinService : ICoinService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public CoinService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public decimal CalculateCoin(decimal price)
        {
            // Lấy tỉ giá cấu hình từ appsettings, mặc định 1000 VNĐ = 1 Xu
            decimal exchangeRate = _configuration.GetValue<decimal>("CoinConfiguration:ExchangeRate", 1000);
            if (exchangeRate <= 0) exchangeRate = 1000;
            return Math.Round(price / exchangeRate, 2);
        }

        public async Task<(bool Success, string Message)> UseCoins(string userId, decimal amount, string description = "")
        {
            var customer = await _context.Customers
                .Include(c => c.Wallet)
                .FirstOrDefaultAsync(c => c.UserId == int.Parse(userId));

            var wallet = customer?.Wallet;

            if (wallet == null)
            {
                return (false, "Không tìm thấy ví của người dùng. Hãy thử khởi tạo ví.");
            }
            if ((wallet.Balance ?? 0) < amount)
            {
                return (false, $"Số dư ({wallet.Balance ?? 0} xu) không đủ để thanh toán {amount} xu.");
            }

            wallet.Balance -= amount;
            wallet.UpdatedDate = DateTime.Now;

            var transaction = new WalletTransaction
            {
                WalletId = wallet.WalletId,
                TransactionCode = $"PAY{DateTime.Now:yyMMddHHmmss}",
                Amount = -amount,
                TransactionType = "Payment",
                Status = "Success",
                CreatedDate = DateTime.Now,
                Description = string.IsNullOrEmpty(description) ? $"Thanh toán đơn hàng: -{amount} xu" : description
            };

            _context.WalletTransactions.Add(transaction);
            await _context.SaveChangesAsync();
            return (true, "Thanh toán bằng xu thành công!");
        }
    }
}