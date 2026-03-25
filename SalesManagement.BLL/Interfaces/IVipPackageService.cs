using SalesManagement.DAL.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SalesManagement.BLL.Interfaces
{
    public interface IVipPackageService
    {
        Task<IEnumerable<VipPackage>> GetAllPackagesAsync();
        Task<VipPackage?> GetPackageByIdAsync(int id);
        Task<bool> AddPackageAsync(VipPackage package);
        Task<bool> UpdatePackageAsync(VipPackage package);
        Task<bool> DeletePackageAsync(int id);
    }
}
