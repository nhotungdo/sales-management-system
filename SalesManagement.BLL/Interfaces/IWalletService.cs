using SalesManagement.DAL.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SalesManagement.BLL.Interfaces
{
    public interface IWalletService
    {
        Task<IEnumerable<WalletTransaction>> GetTransactionsAsync(string? status, DateTime? fromDate, DateTime? toDate);
        Task<bool> ApproveTransactionAsync(int transactionId);
        Task<bool> RejectTransactionAsync(int transactionId);
        Task<IEnumerable<Wallet>> GetAllWalletsAsync(string? search);
        Task<Wallet?> GetWalletByIdAsync(int id);
        Task<bool> AdjustBalanceAsync(int walletId, string type, decimal amount, string reason);
    }
}
