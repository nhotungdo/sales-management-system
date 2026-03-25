using SalesManagement.BLL.Interfaces;
using SalesManagement.DAL.Entities;
using SalesManagement.DAL.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SalesManagement.BLL.Services
{
    public class PromotionService : IPromotionService
    {
        private readonly IRepository<Promotion> _promotionRepository;

        public PromotionService(IRepository<Promotion> promotionRepository)
        {
            _promotionRepository = promotionRepository;
        }

        public async Task<IEnumerable<Promotion>> GetAllPromotionsAsync(string? statusFilter, string? searchString)
        {
            var promotions = await _promotionRepository.GetAllAsync();

            if (!string.IsNullOrEmpty(statusFilter))
            {
                promotions = promotions.Where(p => p.Status == statusFilter);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                promotions = promotions.Where(p => p.Code.Contains(searchString, StringComparison.OrdinalIgnoreCase));
            }

            return promotions.OrderByDescending(p => p.StartDate);
        }

        public async Task<Promotion?> GetPromotionByIdAsync(int id)
        {
            return await _promotionRepository.GetByIdAsync(id);
        }

        public async Task<bool> AddPromotionAsync(Promotion promotion)
        {
            if (!promotion.StartDate.HasValue)
            {
                promotion.StartDate = DateTime.Now;
            }

            if (promotion.EndDate.HasValue && promotion.StartDate >= promotion.EndDate)
            {
                return false;
            }

            if (string.IsNullOrEmpty(promotion.Status))
                promotion.Status = "Active";

            await _promotionRepository.AddAsync(promotion);
            await _promotionRepository.SaveAsync();
            return true;
        }

        public async Task<bool> UpdatePromotionAsync(Promotion promotion)
        {
            var existing = await _promotionRepository.GetByIdAsync(promotion.PromotionId);
            if (existing == null) return false;

            existing.Code = promotion.Code;
            existing.DiscountType = promotion.DiscountType;
            existing.Value = promotion.Value; 
            existing.MinOrderValue = promotion.MinOrderValue;
            existing.MaxDiscountAmount = promotion.MaxDiscountAmount;
            existing.StartDate = promotion.StartDate;
            existing.EndDate = promotion.EndDate;
            existing.Status = promotion.Status;

            _promotionRepository.Update(existing);
            await _promotionRepository.SaveAsync();
            return true;
        }

        public async Task<bool> DeletePromotionAsync(int id)
        {
            var promotion = await _promotionRepository.GetByIdAsync(id);
            if (promotion == null) return false;

            _promotionRepository.Delete(promotion);
            await _promotionRepository.SaveAsync();
            return true;
        }
    }
}
