using SalesManagement.BLL.Interfaces;
using SalesManagement.DAL.Entities;
using SalesManagement.DAL.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SalesManagement.BLL.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IRepository<ProductImage> _imageRepository;
        private readonly ICoinService _coinService;
        private readonly ICurrencyService _currencyService;

        public ProductService(IProductRepository productRepository, IRepository<ProductImage> imageRepository, ICoinService coinService, ICurrencyService currencyService)
        {
            _productRepository = productRepository;
            _imageRepository = imageRepository;
            _coinService = coinService;
            _currencyService = currencyService;
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _productRepository.GetAllAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _productRepository.GetByIdAsync(id);
        }

        public async Task<Product?> GetProductDetailsAsync(int id)
        {
            return await _productRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Product>> GetPagedProductsAsync(int pageNumber, int pageSize, string searchString, string sortOrder)
        {
            var products = await _productRepository.GetAllAsync();
            products = products.Where(p => p.Status != "Deleted");

            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase) || 
                                               p.Description != null && p.Description.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                                               p.Code.Contains(searchString, StringComparison.OrdinalIgnoreCase));
            }

            products = sortOrder switch
            {
                "name_desc" => products.OrderByDescending(p => p.Name),
                "Price" => products.OrderBy(p => p.SellingPrice),
                "price_desc" => products.OrderByDescending(p => p.SellingPrice),
                "Date" => products.OrderBy(p => p.CreatedDate),
                "date_desc" => products.OrderByDescending(p => p.CreatedDate),
                _ => products.OrderByDescending(p => p.CreatedDate)
            };

            return products.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        }

        public async Task<int> GetTotalProductCountAsync(string searchString)
        {
            var products = await _productRepository.GetAllAsync();
            products = products.Where(p => p.Status != "Deleted");

            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase) || 
                                               p.Description != null && p.Description.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                                               p.Code.Contains(searchString, StringComparison.OrdinalIgnoreCase));
            }

            return products.Count();
        }

        public async Task<bool> AddProductAsync(Product product)
        {
            if (await _productRepository.ExistsAsync(p => p.Code == product.Code && p.Status != "Deleted"))
                return false;

            product.CreatedDate = DateTime.Now;
            product.UpdatedDate = DateTime.Now;
            if (string.IsNullOrEmpty(product.Status)) product.Status = "Active";

            product.CoinPrice = _coinService.CalculateCoin(product.SellingPrice);
            product.PriceCents = _currencyService.ConvertVndToCents(product.SellingPrice);

            await _productRepository.AddAsync(product);
            await _productRepository.SaveAsync();
            return true;
        }

        public async Task<bool> UpdateProductAsync(Product product)
        {
            var existing = await _productRepository.GetByIdAsync(product.ProductId);
            if (existing == null) return false;

            if (existing.Code != product.Code)
            {
                if (await _productRepository.ExistsAsync(p => p.Code == product.Code && p.ProductId != product.ProductId && p.Status != "Deleted"))
                    return false;
            }

            existing.Name = product.Name;
            existing.Code = product.Code;
            existing.Description = product.Description;
            existing.SellingPrice = product.SellingPrice;
            existing.StockQuantity = product.StockQuantity;
            existing.CategoryId = product.CategoryId;
            existing.Status = product.Status ?? "Active";
            existing.UpdatedDate = DateTime.Now;

            existing.CoinPrice = _coinService.CalculateCoin(product.SellingPrice);
            existing.PriceCents = _currencyService.ConvertVndToCents(product.SellingPrice);

            _productRepository.Update(existing);
            await _productRepository.SaveAsync();
            return true;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return false;

            product.Status = "Deleted";
            product.UpdatedDate = DateTime.Now;
            
            _productRepository.Update(product);
            await _productRepository.SaveAsync();
            return true;
        }

        public async Task<bool> AddProductImageAsync(int productId, string imageUrl, bool isPrimary)
        {
            var img = new ProductImage
            {
                ProductId = productId,
                ImageUrl = imageUrl,
                IsPrimary = isPrimary,
                CreatedDate = DateTime.Now
            };
            await _imageRepository.AddAsync(img);
            await _imageRepository.SaveAsync();
            return true;
        }

        public async Task<bool> RemoveProductImagesAsync(int productId)
        {
            var images = await _imageRepository.FindAsync(i => i.ProductId == productId);
            foreach (var img in images) _imageRepository.Delete(img);
            await _imageRepository.SaveAsync();
            return true;
        }
    }
}
