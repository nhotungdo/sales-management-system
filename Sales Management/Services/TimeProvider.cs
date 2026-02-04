using System;

namespace Sales_Management.Services
{
    public interface ITimeProvider
    {
        DateTime Now { get; }
        DateOnly Today { get; }
    }

    public class SystemTimeProvider : ITimeProvider
    {
        public DateTime Now => DateTime.Now;
        public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
    }
}
