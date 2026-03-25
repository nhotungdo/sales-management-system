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

        public OrderService(IOrderRepository orderRepository, IProductRepository productRepository, AppDbContext context, ICoinService coinService)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _context = context;
            _coinService = coinService;
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
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null) return false;

            order.Status = status;
            if (status == "Paid") order.PaymentStatus = "Paid";
            
            _orderRepository.Update(order);
            await _orderRepository.SaveAsync();
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
                         CustomerId = customer.CustomerId, // Dùng thực tế
                         OrderDate = DateTime.Now,
                         TotalAmount = product.SellingPrice * quantity,
                         Status = "Completed",
                         PaymentStatus = "Paid",
                         CreatedBy = userId
                     };
                     _context.Orders.Add(order);
                     await _context.SaveChangesAsync();

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
    }
}
