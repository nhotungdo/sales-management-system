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
            _logger.LogInformation("Cart Auto-Release Service đang chạy...");

            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<SalesManagementContext>();
                    var now = DateTime.Now;

                    // 1. Lấy danh sách các sản phẩm trong giỏ đã quá 30 phút mà chưa thanh toán
                    var expiredItems = await context.CartItems
                        .Include(c => c.Product)
                        .Where(c => c.ExpiryTime < now)
                        .ToListAsync();

                    if (expiredItems.Any())
                    {
                        foreach (var item in expiredItems)
                        {
                            if (item.Product != null)
                            {
                                // 2. Hoàn trả số lượng vào kho thực tế
                                item.Product.StockQuantity += item.Quantity;

                                // 3. Ghi nhật ký hoàn kho để Admin theo dõi
                                context.InventoryTransactions.Add(new Models.InventoryTransaction
                                {
                                    ProductId = item.ProductId,
                                    Quantity = item.Quantity,
                                    Type = "Auto-Release-Expired",
                                    CreatedDate = DateTime.Now,
                                    CreatedBy = 0 // Hệ thống tự động
                                });

                                _logger.LogInformation($"Đã hoàn lại {item.Quantity} sản phẩm (ID: {item.ProductId}) do hết hạn giỏ hàng.");
                            }
                        }

                        // 4. Xóa các mục đã hết hạn khỏi bảng CartItems
                        context.CartItems.RemoveRange(expiredItems);
                        await context.SaveChangesAsync();
                    }
                }

                // 5. Nghỉ 1 phút rồi quét lại một lần
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}