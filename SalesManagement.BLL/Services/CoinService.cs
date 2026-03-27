using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SalesManagement.BLL.Interfaces;
using SalesManagement.DAL.Data;
using SalesManagement.DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SalesManagement.BLL.Services
{
    /// <summary>
    /// CoinService – quản lý xu tích lũy của khách hàng.
    /// Coin được lưu vào Wallet.CoinBalance và ghi lịch sử qua WalletTransaction.
    /// </summary>
    public class CoinService : ICoinService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        // Tỉ lệ coin mặc định: 1% giá trị đơn hàng
        private const decimal DefaultCoinRate = 0.01m;

        public CoinService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        /// <inheritdoc/>
        public decimal CalculateCoin(decimal orderAmount)
        {
            var rate = _configuration.GetValue<decimal>("CoinConfiguration:EarnRate", DefaultCoinRate);
            if (rate <= 0 || rate > 1) rate = DefaultCoinRate;
            return Math.Floor(orderAmount * rate); // Làm tròn xuống, 1 đồng là 1 xu
        }

        /// <inheritdoc/>
        public async Task<decimal> GetUserCoins(int userId)
        {
            var wallet = await GetWalletByUserId(userId);
            return wallet?.CoinBalance ?? 0;
        }

        /// <inheritdoc/>
        public async Task<bool> AddCoins(int userId, decimal amount, string description)
        {
            if (amount <= 0) return false;

            var wallet = await GetWalletByUserId(userId);
            if (wallet == null) return false;

            wallet.CoinBalance += amount;
            wallet.UpdatedDate = DateTime.Now;

            _context.WalletTransactions.Add(new WalletTransaction
            {
                WalletId = wallet.WalletId,
                TransactionCode = $"COIN+{DateTime.Now:yyMMddHHmmss}",
                Amount = amount,
                TransactionType = "CoinEarned",
                Status = "Success",
                Method = "System",
                CreatedDate = DateTime.Now,
                Description = string.IsNullOrEmpty(description)
                    ? $"Cộng {amount} xu"
                    : description
            });

            await _context.SaveChangesAsync();
            return true;
        }

        /// <inheritdoc/>
        public async Task<(bool Success, string Message)> DeductCoins(int userId, decimal amount, string description)
        {
            if (amount <= 0) return (false, "Số xu phải lớn hơn 0.");

            var wallet = await GetWalletByUserId(userId);
            if (wallet == null) return (false, "Không tìm thấy ví. Hãy đảm bảo tài khoản đã được khởi tạo.");
            if (wallet.CoinBalance < amount)
                return (false, $"Số xu hiện tại ({wallet.CoinBalance:0} xu) không đủ để trừ {amount:0} xu.");

            wallet.CoinBalance -= amount;
            wallet.UpdatedDate = DateTime.Now;

            _context.WalletTransactions.Add(new WalletTransaction
            {
                WalletId = wallet.WalletId,
                TransactionCode = $"COIN-{DateTime.Now:yyMMddHHmmss}",
                Amount = -amount,
                TransactionType = "CoinUsed",
                Status = "Success",
                Method = "System",
                CreatedDate = DateTime.Now,
                Description = string.IsNullOrEmpty(description)
                    ? $"Trừ {amount} xu"
                    : description
            });

            await _context.SaveChangesAsync();
            return (true, "Trừ xu thành công.");
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<WalletTransaction>> GetCoinTransactionHistory(int userId)
        {
            var wallet = await GetWalletByUserId(userId);
            if (wallet == null) return Enumerable.Empty<WalletTransaction>();

            return await _context.WalletTransactions
                .Where(t => t.WalletId == wallet.WalletId
                         && (t.TransactionType == "CoinEarned" || t.TransactionType == "CoinUsed"))
                .OrderByDescending(t => t.CreatedDate)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<(bool Success, string Message)> UseCoins(string userId, decimal amount, string description = "")
        {
            if (!int.TryParse(userId, out var uid))
                return (false, "UserId không hợp lệ.");

            return await DeductCoins(uid, amount, string.IsNullOrEmpty(description)
                ? $"Dùng {amount} xu để giảm giá đơn hàng"
                : description);
        }

        // ─── Private helpers ─────────────────────────────────────────────────────────

        private async Task<Wallet?> GetWalletByUserId(int userId)
        {
            return await _context.Wallets
                .Include(w => w.Customer)
                .FirstOrDefaultAsync(w => w.Customer.UserId == userId);
        }
    }
}
