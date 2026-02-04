using System.Threading.Tasks;
using Sales_Management.Models;

namespace Sales_Management.Services
{
    public interface IPayrollService
    {
        Task GeneratePayrollForAllAsync(int month, int year);
        Task<Payroll> CalculatePayrollForEmployeeAsync(int employeeId, int month, int year);
    }
}
