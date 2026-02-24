using Microsoft.EntityFrameworkCore;
using Sales_Management.Data;

namespace Sales_Management.Services
{
    public class CartAutoReleaseService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<CartAutoReleaseService> _logger;

        public CartAutoReleaseService(IServiceProvider services, ILogger<CartAutoReleaseService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Cart Auto-Release Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _services.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<SalesManagementContext>();
                        var now = DateTime.Now;

                        // 1. Lấy danh sách hết hạn
                        var expiredItems = await context.CartItems
                            .Include(c => c.Product)
                            .Where(c => c.ExpiryTime < now)
                            .ToListAsync(stoppingToken);

                        if (expiredItems.Any())
                        {
                            foreach (var item in expiredItems)
                            {
                                if (item.Product != null)
                                {
                                    // 2. Hoàn tồn kho
                                    item.Product.StockQuantity += item.Quantity;

                                    // 3. Ghi lịch sử kho
                                    context.InventoryTransactions.Add(new Models.InventoryTransaction
                                    {
                                        ProductId = item.ProductId,
                                        Quantity = item.Quantity,
                                        Type = "Auto-Release",
                                        CreatedDate = DateTime.Now,
                                        CreatedBy = 3 // Đảm bảo ID này luôn tồn tại trong bảng Users
                                    });
                                }
                            }

                            // 4. Xóa khỏi giỏ và lưu thay đổi
                            context.CartItems.RemoveRange(expiredItems);
                            await context.SaveChangesAsync(stoppingToken);

                            _logger.LogInformation($"Successfully released {expiredItems.Count} expired cart items.");
                        }
                    }
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    _logger.LogError(ex, "Error occurred while releasing expired cart items.");
                }

                // Quét lại sau mỗi 1 phút
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}