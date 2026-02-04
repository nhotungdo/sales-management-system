using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Services;
using Sales_Management.Data;
using Sales_Management.Models;
using Microsoft.AspNetCore.SignalR;
using Sales_Management.Hubs;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Sales_Management.Tests
{
    public class AuthServiceTests
    {
        private SalesManagementContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<SalesManagementContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new SalesManagementContext(options);
        }

        [Fact]
        public async Task CheckOutSalesEmployee_EarlyWithoutReason_SetsEarlyStatus()
        {
            // Arrange
            var context = GetInMemoryContext();
            var mockHub = new Mock<IHubContext<SystemHub>>();
            var service = new AuthService(context, mockHub.Object);
            
            // Setup Data
            var user = new User { UserId = 10, Username = "test", Role = "Sales", IsActive = true };
            var emp = new Employee { EmployeeId = 10, UserId = 10, ShiftId = 10 };
            user.Employee = emp;
            context.Users.Add(user);
            context.Employees.Add(emp);
            
            // Shift ends in future (so it is early)
            var shift = new Shift { ShiftId = 10, StartTime = TimeSpan.MinValue, EndTime = DateTime.Now.AddHours(2).TimeOfDay };
            context.Shifts.Add(shift);

            // Active Session
            var session = new TimeAttendance { AttendanceId = 10, EmployeeId = 10, Date = DateOnly.FromDateTime(DateTime.Now), CheckInTime = DateTime.Now.AddHours(-1), ShiftId = 10 };
            context.TimeAttendances.Add(session);
            
            await context.SaveChangesAsync();

            // Act
            await service.CheckOutSalesEmployee(10, "");

            // Assert
            var result = await context.TimeAttendances.FindAsync(10);
            Assert.NotNull(result.CheckOutTime);
            Assert.Contains("Early", result.Status);
        }

        [Fact]
        public async Task CheckOutSalesEmployee_WithReason_SetsCheckedOutStatus()
        {
            // Arrange
            var context = GetInMemoryContext();
            var mockHub = new Mock<IHubContext<SystemHub>>();
            var service = new AuthService(context, mockHub.Object);
            
            var user = new User { UserId = 20, Username = "test2", Role = "Sales", IsActive = true };
            var emp = new Employee { EmployeeId = 20, UserId = 20, ShiftId = 20 };
            user.Employee = emp;
            context.Users.Add(user);
            context.Employees.Add(emp);
            
            var shift = new Shift { ShiftId = 20, StartTime = TimeSpan.MinValue, EndTime = DateTime.Now.AddHours(2).TimeOfDay };
            context.Shifts.Add(shift);

            var session = new TimeAttendance { AttendanceId = 20, EmployeeId = 20, Date = DateOnly.FromDateTime(DateTime.Now), CheckInTime = DateTime.Now.AddHours(-1), ShiftId = 20 };
            context.TimeAttendances.Add(session);
            
            await context.SaveChangesAsync();

            // Act
            await service.CheckOutSalesEmployee(20, "Sick leave");

            // Assert
            var result = await context.TimeAttendances.FindAsync(20);
            Assert.Equal("CheckedOut", result.Status);
            Assert.Equal("Sick leave", result.Notes);
        }
    }
}
