using SalesManagement.DAL.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SalesManagement.BLL.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<Product?> GetProductByIdAsync(int id);
        Task<Product?> GetProductDetailsAsync(int id);
        Task<IEnumerable<Product>> GetPagedProductsAsync(int pageNumber, int pageSize, string searchString, string sortOrder);
        Task<int> GetTotalProductCountAsync(string searchString);
        Task<bool> AddProductAsync(Product product);
        Task<bool> UpdateProductAsync(Product product);
        Task<bool> DeleteProductAsync(int id);
        Task<bool> AddProductImageAsync(int productId, string imageUrl, bool isPrimary);
        Task<bool> RemoveProductImagesAsync(int productId);
    }
}
