using SalesManagement.BLL.Interfaces;
using SalesManagement.DAL.Entities;
using SalesManagement.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using SalesManagement.DAL.Data;

namespace SalesManagement.BLL.Services
{
    public class WalletService : IWalletService
    {
        private readonly IRepository<WalletTransaction> _transactionRepository;
        private readonly IRepository<Wallet> _walletRepository;
        private readonly AppDbContext _context; // To include navigation properties in repo? Better to use specialized repo.

        public WalletService(IRepository<WalletTransaction> transactionRepository, IRepository<Wallet> walletRepository, AppDbContext context)
        {
            _transactionRepository = transactionRepository;
            _walletRepository = walletRepository;
            _context = context;
        }

        public async Task<IEnumerable<WalletTransaction>> GetTransactionsAsync(string? status, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.WalletTransactions
                .Include(t => t.Wallet)
                .ThenInclude(w => w.Customer)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status)) query = query.Where(t => t.Status == status);
            if (fromDate.HasValue) query = query.Where(t => t.CreatedDate >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(t => t.CreatedDate <= toDate.Value.AddDays(1));

            return await query.OrderByDescending(t => t.CreatedDate).ToListAsync();
        }

        public async Task<bool> ApproveTransactionAsync(int transactionId)
        {
            var transaction = await _context.WalletTransactions
                .Include(t => t.Wallet)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

            if (transaction != null && transaction.Status == "Pending")
            {
                transaction.Status = "Success";
                transaction.Wallet.Balance = (transaction.Wallet.Balance ?? 0) + transaction.Amount;
                transaction.Wallet.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> RejectTransactionAsync(int transactionId)
        {
            var transaction = await _transactionRepository.GetByIdAsync(transactionId);
            if (transaction != null && transaction.Status == "Pending")
            {
                transaction.Status = "Cancelled";
                await _transactionRepository.SaveAsync();
                return true;
            }
            return false;
        }

        public async Task<IEnumerable<Wallet>> GetAllWalletsAsync(string? search)
        {
            var query = _context.Wallets.Include(w => w.Customer).AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(w => w.Customer.FullName.ToLower().Contains(search) 
                                      || w.Customer.Email.ToLower().Contains(search) 
                                      || w.Customer.PhoneNumber.Contains(search));
            }
            return await query.OrderByDescending(w => w.Balance).ToListAsync();
        }

        public async Task<Wallet?> GetWalletByIdAsync(int id)
        {
             return await _context.Wallets.Include(w => w.Customer).FirstOrDefaultAsync(w => w.WalletId == id);
        }

        public async Task<bool> AdjustBalanceAsync(int walletId, string type, decimal amount, string reason)
        {
            var wallet = await _context.Wallets.Include(w => w.Customer).FirstOrDefaultAsync(w => w.WalletId == walletId);
            if (wallet == null || amount <= 0) return false;

            decimal adjustment = (type == "add") ? amount : -amount;
            if (type == "deduct" && (wallet.Balance ?? 0) < amount) return false;

            var transaction = new WalletTransaction
            {
                WalletId = walletId,
                Amount = adjustment,
                TransactionType = "Adjustment",
                Method = "System",
                Status = "Success",
                TransactionCode = $"ADJ{DateTime.Now:yyMMddHHmmss}{new Random().Next(100, 999)}",
                Description = $"Admin chỉnh sửa: {reason}",
                CreatedDate = DateTime.Now
            };

            wallet.Balance = (wallet.Balance ?? 0) + adjustment;
            wallet.UpdatedDate = DateTime.Now;

            _context.WalletTransactions.Add(transaction);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
