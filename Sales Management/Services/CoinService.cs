using Microsoft.Extensions.Configuration;
using System;

namespace Sales_Management.Services
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
            int exchangeRate = _configuration.GetValue<int>("CoinConfiguration:ExchangeRate", 1000);
            if (exchangeRate <= 0) return 0;
            
            return (int)Math.Round(price / exchangeRate);
        }
    }
}
