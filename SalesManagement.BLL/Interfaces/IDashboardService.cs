using SalesManagement.BLL.Models;
using System.Threading.Tasks;

namespace SalesManagement.BLL.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardDTO> GetDashboardStatsAsync();
    }
}
