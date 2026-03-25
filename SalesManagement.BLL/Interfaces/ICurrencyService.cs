using System.Threading.Tasks;

namespace SalesManagement.BLL.Interfaces
{
    public interface ICurrencyService
    {
        decimal ConvertVndToCents(decimal vndAmount);
        Task LogConversionAsync(decimal vnd, decimal cents, string ipAddress, bool success, string message);
    }
}
