using Microsoft.Extensions.Configuration;
using System;
using SalesManagement.BLL.Interfaces;

namespace SalesManagement.BLL.Services
{
    public class CoinService : ICoinService
    {
        private readonly IConfiguration _configuration;

        public CoinService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public int CalculateCoin(decimal price)
        {
            int exchangeRate = _configuration.GetValue("CoinConfiguration:ExchangeRate", 1000);
            if (exchangeRate <= 0) return 0;
            
            return (int)Math.Round(price / exchangeRate);
        }
    }
}
