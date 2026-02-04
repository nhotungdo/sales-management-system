using System.Threading.Tasks;

namespace Sales_Management.Services
{
    public interface ICurrencyService
    {
        decimal ConvertVndToCents(decimal vndAmount);
        Task LogConversionAsync(decimal vnd, decimal cents, string ipAddress, bool success, string message);
    }
}
