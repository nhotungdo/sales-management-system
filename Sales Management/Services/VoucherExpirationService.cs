using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Data;
using Sales_Management.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sales_Management.Services
{
    public class VoucherExpirationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<VoucherExpirationService> _logger;

        public VoucherExpirationService(IServiceProvider serviceProvider, ILogger<VoucherExpirationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Voucher Expiration Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckAndExpireVouchers();

                // Check every minute for testing, or use a longer interval in production
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task CheckAndExpireVouchers()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<SalesManagementContext>();

                try
                {
                    var now = DateTime.Now;
                    
                    // Find active promotions that have passed their end date
                    var expiredPromotions = await context.Promotions
                        .Where(p => p.Status == "Active" && p.EndDate < now)
                        .ToListAsync();

                    if (expiredPromotions.Any())
                    {
                        foreach (var promotion in expiredPromotions)
                        {
                            promotion.Status = "Disabled";
                            _logger.LogInformation($"Voucher {promotion.Code} expired at {now}. Status updated to Disabled.");
                        }

                        await context.SaveChangesAsync();
                        _logger.LogInformation($"Expired {expiredPromotions.Count} vouchers.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking for expired vouchers.");
                }
            }
        }
    }
}
