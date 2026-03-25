using SalesManagement.BLL.Interfaces;
using SalesManagement.DAL.Entities;
using SalesManagement.DAL.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SalesManagement.BLL.Services
{
    public class VipPackageService : IVipPackageService
    {
        private readonly IRepository<VipPackage> _vipPackageRepository;

        public VipPackageService(IRepository<VipPackage> vipPackageRepository)
        {
            _vipPackageRepository = vipPackageRepository;
        }

        public async Task<IEnumerable<VipPackage>> GetAllPackagesAsync()
        {
            var packages = await _vipPackageRepository.GetAllAsync();
            return packages.OrderBy(p => p.Price);
        }

        public async Task<VipPackage?> GetPackageByIdAsync(int id)
        {
            return await _vipPackageRepository.GetByIdAsync(id);
        }

        public async Task<bool> AddPackageAsync(VipPackage package)
        {
            await _vipPackageRepository.AddAsync(package);
            await _vipPackageRepository.SaveAsync();
            return true;
        }

        public async Task<bool> UpdatePackageAsync(VipPackage package)
        {
            var existing = await _vipPackageRepository.GetByIdAsync(package.VipPackageId);
            if (existing == null) return false;

            existing.Name = package.Name;
            existing.Tag = package.Tag;
            existing.Description = package.Description;
            existing.Price = package.Price;
            existing.DurationMonth = package.DurationMonth;
            existing.DiscountPercent = package.DiscountPercent;
            existing.Features = package.Features;
            existing.Status = package.Status;

            _vipPackageRepository.Update(existing);
            await _vipPackageRepository.SaveAsync();
            return true;
        }

        public async Task<bool> DeletePackageAsync(int id)
        {
            var package = await _vipPackageRepository.GetByIdAsync(id);
            if (package == null) return false;

            _vipPackageRepository.Delete(package);
            await _vipPackageRepository.SaveAsync();
            return true;
        }
    }
}
