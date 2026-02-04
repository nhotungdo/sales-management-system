using Sales_Management.Data;
using Sales_Management.Models;
using System;
using System.Threading.Tasks;

namespace Sales_Management.Services
{
    public class CurrencyService : ICurrencyService
    {
        private readonly SalesManagementContext _context;
        private const decimal ExchangeRate = 1000m; // 1000 VND = 1 Cent

        public CurrencyService(SalesManagementContext context)
        {
            _context = context;
        }

        public decimal ConvertVndToCents(decimal vndAmount)
        {
            if (vndAmount < 0)
                throw new ArgumentException("Amount must be positive.");
            if (vndAmount > 999999999)
                throw new ArgumentException("Amount exceeds maximum limit of 999,999,999 VND.");

            // 1 cent = 1000 VND => Cents = VND / 1000
            return Math.Round(vndAmount / ExchangeRate, 2);
        }

        public async Task LogConversionAsync(decimal vnd, decimal cents, string ipAddress, bool success, string message)
        {
            var log = new ConversionAuditLog
            {
                VndAmount = vnd,
                CentsAmount = cents,
                ConversionRate = ExchangeRate,
                IpAddress = ipAddress,
                IsSuccess = success,
                ErrorMessage = message,
                Timestamp = DateTime.UtcNow
            };
            
            _context.ConversionAuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
