using System.Collections.Generic;
using System.Threading.Tasks;
using SalesManagement.DAL.Entities;

namespace SalesManagement.BLL.Interfaces
{
    public interface ICoinService
    {
        /// <summary>Tính số coin được cộng theo giá trị đơn hàng (ví dụ 1% giá trị)</summary>
        decimal CalculateCoin(decimal orderAmount);

        /// <summary>Lấy tổng số coin hiện tại của user (qua Wallet.CoinBalance)</summary>
        Task<decimal> GetUserCoins(int userId);

        /// <summary>Cộng coin khi mua hàng</summary>
        Task<bool> AddCoins(int userId, decimal amount, string description);

        /// <summary>Trừ coin khi hủy đơn hoặc dùng coin</summary>
        Task<(bool Success, string Message)> DeductCoins(int userId, decimal amount, string description);

        /// <summary>Lấy lịch sử giao dịch coin (WalletTransactions type=Coin*)</summary>
        Task<IEnumerable<WalletTransaction>> GetCoinTransactionHistory(int userId);

        /// <summary>Dùng coin để giảm giá khi checkout (giảm trực tiếp từ Wallet.CoinBalance)</summary>
        Task<(bool Success, string Message)> UseCoins(string userId, decimal amount, string description = "");
    }
}
