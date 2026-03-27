using Microsoft.EntityFrameworkCore;
using SalesManagement.BLL.Interfaces;
using SalesManagement.DAL.Data;
using SalesManagement.DAL.Entities;
using SalesManagement.DAL.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SalesManagement.BLL.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;
        private readonly AppDbContext _context;
        private readonly ICoinService _coinService;
        private readonly IPromotionService _promotionService;

        public OrderService(IOrderRepository orderRepository, IProductRepository productRepository, AppDbContext context, ICoinService coinService, IPromotionService promotionService)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _context = context;
            _coinService = coinService;
            _promotionService = promotionService;
        }

        public async Task<IEnumerable<Order>> GetOrdersOverviewAsync(string? searchString, string? statusFilter)
        {
            return await _orderRepository.GetOrdersWithCustomerAsync(searchString, statusFilter);
        }

        public async Task<Order?> GetOrderDetailsAsync(int id)
        {
            return await _orderRepository.GetOrderDetailsAsync(id);
        }

        public async Task<IEnumerable<Order>> GetMyOrdersAsync(int userId)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null) return new List<Order>();

            return await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                .Where(o => o.CustomerId == customer.CustomerId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<bool> UpdateOrderStatusAsync(int id, string status)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == id);
            if (order == null) return false;

            if (status == "Cancelled" && (order.PaymentStatus == "Paid" || order.PaymentStatus == "Refunded"))
            {
                // If it's already refunded, don't do anything (or just update status if not already Cancelled)
                if (order.PaymentStatus == "Refunded" && order.Status == "Cancelled") return true;

                // Trigger the full refund and cancellation logic
                return await RefundOrderAsync(id);
            }

            order.Status = status;
            if (status == "Paid" || status == "Completed")
            {
                order.PaymentStatus = "Paid";
            }

            // Sync with Invoice
            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.OrderId == id);
            if (invoice != null)
            {
                if (status == "Paid" || status == "Completed")
                    invoice.Status = "Paid";
                else if (status == "Cancelled")
                    invoice.Status = "Cancelled";
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Order?> CreateOrderAsync(int customerId, List<int> productIds, List<int> quantities)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var order = new Order
                    {
                        CustomerId = customerId,
                        OrderDate = DateTime.Now,
                        Status = "Completed",
                        PaymentStatus = "Unpaid",
                        CreatedBy = 1 // Hardcoded or from Claims
                    };

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();

                    decimal subTotal = 0;

                    for (int i = 0; i < productIds.Count; i++)
                    {
                        var product = await _context.Products.FindAsync(productIds[i]);
                        if (product == null || quantities[i] <= 0 || quantities[i] > (product.StockQuantity ?? 0))
                            continue;

                        var detail = new OrderDetail
                        {
                            OrderId = order.OrderId,
                            ProductId = product.ProductId,
                            Quantity = quantities[i],
                            UnitPrice = product.SellingPrice,
                            Total = quantities[i] * product.SellingPrice
                        };

                        subTotal += detail.Total ?? 0;
                        product.StockQuantity -= quantities[i];
                        _context.OrderDetails.Add(detail);
                        
                        _context.InventoryTransactions.Add(new InventoryTransaction
                        {
                             ProductId = product.ProductId,
                             Quantity = -quantities[i],
                             CreatedDate = DateTime.Now,
                             Type = "Sale",
                             CreatedBy = 1
                        });
                    }

                    order.SubTotal = subTotal;
                    order.TaxAmount = subTotal * 0.1m;
                    order.TotalAmount = order.SubTotal + order.TaxAmount;

                    var invoice = new Invoice
                    {
                        OrderId = order.OrderId,
                        InvoiceDate = DateTime.Now,
                        Amount = order.TotalAmount ?? 0,
                        Status = "Unpaid"
                    };
                    _context.Invoices.Add(invoice);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return order;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    return null;
                }
            }
        }

        public async Task<(bool Success, string Message, int OrderId)> CheckoutAsync(int userId, int productId, int quantity)
        {
             using (var transaction = await _context.Database.BeginTransactionAsync())
             {
                 try
                 {
                     var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId);
                     if (product == null) return (false, "Sản phẩm không tồn tại!", 0);

                     if (product.StockQuantity < quantity)
                         return (false, "Sản phẩm không đủ số lượng trong kho!", 0);
                     
                     // Map UserId to CustomerId
                     var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
                     if (customer == null)
                     {
                         return (false, "Bạn cần hoàn thiện hồ sơ khách hàng trước khi mua hàng!", 0);
                     }

                     // Using exact CoinPrice stored in product (matches the UI)
                     decimal priceInCoins = (product.CoinPrice ?? 0) * quantity;
                     var coinResult = await _coinService.UseCoins(userId.ToString(), priceInCoins);
                     
                     if (!coinResult.Success)
                         return (false, coinResult.Message, 0);

                     // Update Stock
                     product.StockQuantity -= quantity;

                     // Create Order
                     var order = new Order
                     {
                         CustomerId = customer.CustomerId,
                         OrderDate = DateTime.Now,
                         TotalAmount = product.SellingPrice * quantity,
                         Status = "Confirmed",
                         PaymentStatus = "Paid",
                         PaymentMethod = "Wallet",
                         CreatedBy = userId
                     };
                     _context.Orders.Add(order);
                     await _context.SaveChangesAsync();

                     // Create Invoice
                     var invoice = new Invoice
                     {
                         OrderId = order.OrderId,
                         Amount = (product.SellingPrice * quantity),
                         InvoiceDate = DateTime.Now,
                         Status = "Paid"
                     };
                     _context.Invoices.Add(invoice);
                     await _context.SaveChangesAsync();

                     // Link Wallet Transaction
                     var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.CustomerId == customer.CustomerId);
                     if (wallet != null)
                     {
                         var lastTrans = await _context.WalletTransactions
                             .Where(t => t.WalletId == wallet.WalletId)
                             .OrderByDescending(t => t.CreatedDate)
                             .FirstOrDefaultAsync();
                         
                         if (lastTrans != null)
                         {
                             lastTrans.InvoiceId = invoice.InvoiceId;
                             lastTrans.Description = $"Thanh toán hóa đơn #INV-{invoice.InvoiceId:D5} cho đơn hàng mua ngay #ORD-{order.OrderId:D5}";
                         }
                     }

                     // Detail
                     var detail = new OrderDetail
                     {
                         OrderId = order.OrderId,
                         ProductId = productId,
                         Quantity = quantity,
                         UnitPrice = product.SellingPrice,
                         Total = product.SellingPrice * quantity
                     };
                     _context.OrderDetails.Add(detail);

                     // Inventory log
                     _context.InventoryTransactions.Add(new InventoryTransaction
                     {
                         ProductId = productId,
                         Quantity = -quantity,
                         CreatedDate = DateTime.Now,
                         Type = "DirectSale",
                         CreatedBy = userId
                     });

                     await _context.SaveChangesAsync();
                     await transaction.CommitAsync();

                     return (true, "Mua hàng thành công!", order.OrderId);
                 }
                 catch (Exception ex)
                 {
                     await transaction.RollbackAsync();
                     return (false, $"Lỗi hệ thống: {ex.Message}", 0);
                 }
             }
        }
        public async Task<(bool Success, string Message, int OrderId)> CheckoutCartAsync(int userId, List<(int productId, int quantity)> items, bool usePoints = false, string? promoCode = null)
        {
            if (items == null || !items.Any())
                return (false, "Giỏ hàng trống!", 0);

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var customer = await _context.Customers.Include(c => c.Wallet).FirstOrDefaultAsync(c => c.UserId == userId);
                    if (customer == null)
                        return (false, "Bạn cần hoàn thiện hồ sơ khách hàng trước khi mua hàng!", 0);

                    decimal totalOrderAmount = 0;
                    var processedItems = new List<(Product product, int quantity)>();

                    // 1. Validate all items and calculate total order cash volume
                    foreach (var item in items)
                    {
                        var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == item.productId);
                        if (product == null)
                            return (false, $"Sản phẩm ID {item.productId} không tồn tại!", 0);

                        if (product.StockQuantity < item.quantity)
                            return (false, $"Sản phẩm '{product.Name}' không đủ số lượng trong kho (Còn {product.StockQuantity})!", 0);

                        totalOrderAmount += product.SellingPrice * item.quantity;
                        processedItems.Add((product, item.quantity));
                    }

                    decimal discountFromCoins = 0;
                    decimal discountFromPromo = 0;

                    // 2. Process Promotions if any
                    if (!string.IsNullOrEmpty(promoCode))
                    {
                        var promoResult = await _promotionService.ValidatePromotionAsync(promoCode, totalOrderAmount);
                        if (!promoResult.Success)
                            return (false, promoResult.Message, 0);
                        
                        discountFromPromo = promoResult.DiscountAmount;
                    }

                    // 2.1 Process Coins/Points if requested (after promo discount)
                    decimal remainingAfterPromo = totalOrderAmount - discountFromPromo;

                    if (usePoints)
                    {
                        var availableCoins = await _coinService.GetUserCoins(userId);
                        if (availableCoins > 0)
                        {
                            // 1 xu quy đổi = 1 VND
                            discountFromCoins = Math.Min(availableCoins, remainingAfterPromo);
                            
                            var coinResult = await _coinService.UseCoins(userId.ToString(), discountFromCoins, "Dùng xu tích lũy để thanh toán đơn hàng");
                            if (!coinResult.Success)
                                return (false, coinResult.Message, 0);
                        }
                    }

                    decimal finalPayableAmount = totalOrderAmount - discountFromPromo - discountFromCoins;
                    decimal totalDiscount = discountFromPromo + discountFromCoins;

                    // 3. Create single order
                    var order = new Order
                    {
                        CustomerId = customer.CustomerId,
                        OrderDate = DateTime.Now,
                        TotalAmount = finalPayableAmount,
                        DiscountAmount = totalDiscount,
                        Status = "Confirmed",
                        PaymentStatus = "Paid",
                        PaymentMethod = "Wallet",
                        CreatedBy = userId
                    };
                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();

                    // 3.1 Subtract REAL money from Wallet (if any remaining amount to pay)
                    if (finalPayableAmount > 0)
                    {
                        var wallet = customer.Wallet;
                        if (wallet == null || wallet.Balance < finalPayableAmount)
                            return (false, "Số dư ví không đủ để thanh toán phần còn lại của đơn hàng!", 0);

                        wallet.Balance -= finalPayableAmount;
                        wallet.UpdatedDate = DateTime.Now;

                        _context.WalletTransactions.Add(new WalletTransaction
                        {
                            WalletId = wallet.WalletId,
                            Amount = -finalPayableAmount,
                            TransactionType = "Payment",
                            Status = "Success",
                            Method = "System",
                            CreatedDate = DateTime.Now,
                            Description = $"Thanh toán thực tế cho đơn hàng #ORD-{order.OrderId:D5} (Tổng giảm: {totalDiscount} xu, gồm {discountFromPromo} mã KM và {discountFromCoins} xu tích lũy)"
                        });
                    }

                    // 3.2 Create Invoice
                    var invoice = new Invoice
                    {
                        OrderId = order.OrderId,
                        Amount = finalPayableAmount,
                        InvoiceDate = DateTime.Now,
                        Status = "Paid"
                    };
                    _context.Invoices.Add(invoice);
                    await _context.SaveChangesAsync();

                    // 3.3 Link the transactions to the invoice
                    var lastCashTrans = await _context.WalletTransactions
                        .Where(t => t.WalletId == customer.Wallet.WalletId && t.TransactionType == "Payment")
                        .OrderByDescending(t => t.CreatedDate)
                        .FirstOrDefaultAsync();
                    
                    if (lastCashTrans != null) lastCashTrans.InvoiceId = invoice.InvoiceId;

                    // 4. Create details and inventory updates
                    foreach (var item in processedItems)
                    {
                        // Update stock
                        item.product.StockQuantity -= item.quantity;

                        // Create Detail
                        var detail = new OrderDetail
                        {
                            OrderId = order.OrderId,
                            ProductId = item.product.ProductId,
                            Quantity = item.quantity,
                            UnitPrice = item.product.SellingPrice,
                            Total = item.product.SellingPrice * item.quantity
                        };
                        _context.OrderDetails.Add(detail);

                        // Inventory log
                        _context.InventoryTransactions.Add(new InventoryTransaction
                        {
                            ProductId = item.product.ProductId,
                            Quantity = -item.quantity,
                            CreatedDate = DateTime.Now,
                            Type = "Sale",
                            Note = $"Order #{order.OrderId:D5}",
                            CreatedBy = userId
                        });
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Cộng coin tích lũy: 1% giá trị đơn hàng (trừ phần) 
                    var earnedCoins = _coinService.CalculateCoin(totalOrderAmount);
                    if (earnedCoins > 0)
                    {
                        // Commit đã xong, dùng best-effort (không throw nếu lỗi coin)
                        try { await _coinService.AddCoins(userId, earnedCoins, $"Thưởng xu đơn hàng #ORD-{order.OrderId:D5}"); }
                        catch { /* Không block checkout */ }
                    }

                    return (true, "Thanh toán thành công!", order.OrderId);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return (false, $"Lỗi hệ thống: {ex.Message}", 0);
                }
            }
        }
        
        public async Task<bool> RefundOrderAsync(int id)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var order = await _context.Orders
                        .Include(o => o.Customer)
                        .Include(o => o.OrderDetails)
                            .ThenInclude(d => d.Product)
                        .Include(o => o.OrderPromotions)
                        .Include(o => o.Invoices)
                        .FirstOrDefaultAsync(o => o.OrderId == id);

                    if (order == null || order.Status == "Cancelled" || order.PaymentStatus == "Refunded")
                        return false;

                    var customer = order.Customer;
                    var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.CustomerId == customer.CustomerId);

                    // 1. Calculate and Refund amounts
                    decimal promoDiscount = order.OrderPromotions.Sum(p => p.AppliedValue);
                    decimal coinRefund = (order.DiscountAmount ?? 0) - promoDiscount;
                    decimal cashRefund = order.TotalAmount ?? 0;

                    if (wallet != null)
                    {
                        // Refund Cash to Wallet Balance
                        if (cashRefund > 0)
                        {
                            wallet.Balance += cashRefund;
                            _context.WalletTransactions.Add(new WalletTransaction
                            {
                                WalletId = wallet.WalletId,
                                Amount = cashRefund,
                                TransactionType = "Refund",
                                Status = "Success",
                                Method = "System",
                                Description = $"Hoàn tiền thực tế cho đơn hàng #ORD-{order.OrderId:D5}",
                                CreatedDate = DateTime.Now
                            });
                        }

                        // Refund Coins used for discount
                        if (coinRefund > 0)
                        {
                            await _coinService.AddCoins(customer.UserId!.Value, coinRefund, $"Hoàn lại xu dùng cho đơn hàng #ORD-{order.OrderId:D5}");
                        }

                        // Deduct coins earned from this order (1% by default)
                        // We check the original cash amount to deduct the same amount that was added
                        decimal earnedToTakeBack = _coinService.CalculateCoin(order.SubTotal ?? (cashRefund + promoDiscount + coinRefund));
                        if (earnedToTakeBack > 0)
                        {
                            await _coinService.DeductCoins(customer.UserId!.Value, earnedToTakeBack, $"Khấu trừ xu từ đơn hàng hoàn trả #ORD-{order.OrderId:D5}");
                        }
                    }

                    // 2. Restore Stock
                    foreach (var item in order.OrderDetails)
                    {
                        if (item.Product != null)
                        {
                            item.Product.StockQuantity += item.Quantity;
                            _context.InventoryTransactions.Add(new InventoryTransaction
                            {
                                ProductId = item.ProductId,
                                Quantity = item.Quantity,
                                Type = "Import", 
                                Note = $"Hoàn kho từ đơn hàng hoàn tiền #ORD-{order.OrderId:D5}",
                                CreatedDate = DateTime.Now,
                                CreatedBy = order.CreatedBy
                            });
                        }
                    }

                    // 3. Update Order and Invoice Status
                    order.Status = "Cancelled";
                    order.PaymentStatus = "Refunded";

                    foreach (var invoice in order.Invoices)
                    {
                        invoice.Status = "Cancelled";
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    return false;
                }
            }
        }
    }
}
