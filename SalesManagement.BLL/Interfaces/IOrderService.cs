using SalesManagement.DAL.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SalesManagement.BLL.Interfaces
{
    public interface IOrderService
    {
        Task<IEnumerable<Order>> GetOrdersOverviewAsync(string? searchString, string? statusFilter);
        Task<Order?> GetOrderDetailsAsync(int id);
        Task<bool> UpdateOrderStatusAsync(int id, string status);
        Task<Order?> CreateOrderAsync(int customerId, List<int> productIds, List<int> quantities);
    }
}
