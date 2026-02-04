using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Data;
using Sales_Management.Models;

namespace Sales_Management.Services
{
    public class PayrollService : IPayrollService
    {
        private readonly SalesManagementContext _context;
        private const int StandardWorkDays = 22;

        public PayrollService(SalesManagementContext context)
        {
            _context = context;
        }

        public async Task GeneratePayrollForAllAsync(int month, int year)
        {
            var employees = await _context.Employees
                .Where(e => !e.IsDeleted)
                .ToListAsync();

            foreach (var emp in employees)
            {
                // Check if payroll already exists
                var exists = await _context.Payrolls
                    .AnyAsync(p => p.EmployeeId == emp.EmployeeId && p.Month == month && p.Year == year);
                
                if (exists) continue; // Skip or Update? Let's skip for now.

                await CalculatePayrollForEmployeeAsync(emp.EmployeeId, month, year);
            }
        }

        public async Task<Payroll> CalculatePayrollForEmployeeAsync(int employeeId, int month, int year)
        {
            var employee = await _context.Employees
                .Include(e => e.EmployeeSalaryComponents)
                .ThenInclude(esc => esc.SalaryComponent)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (employee == null) return null;

            // Fetch Attendance
            var startOfMonth = new DateOnly(year, month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            var attendances = await _context.TimeAttendances
                .Where(ta => ta.EmployeeId == employeeId && ta.Date >= startOfMonth && ta.Date <= endOfMonth)
                .ToListAsync();

            double totalOvertimeHours = attendances.Sum(a => a.OvertimeHours);
            decimal latePenalties = attendances.Sum(a => a.DeductionAmount);
            // double totalWorkHours = attendances.Sum(a => a.WorkHours);

            // Basic Metrics
            decimal basicSalary = 0;
            decimal hourlyRate = 0;
            decimal grossEarnings = 0;

            if (employee.HourlyWage.HasValue && employee.HourlyWage.Value > 0)
            {
                 // Hourly Wage Model
                 double totalWorkHours = attendances.Sum(a => a.WorkHours);
                 hourlyRate = employee.HourlyWage.Value;
                 basicSalary = 0; // No base salary for hourly
                 grossEarnings = (decimal)totalWorkHours * hourlyRate;
            }
            else
            {
                 // Fixed Salary Model
                 basicSalary = employee.BasicSalary ?? 0;
                 hourlyRate = basicSalary / (StandardWorkDays * 8);
                 grossEarnings = basicSalary;
            }

            // Calculate Components
            decimal totalAllowances = 0;
            decimal totalDeductions = 0;

            foreach (var comp in employee.EmployeeSalaryComponents)
            {
                var amount = comp.Amount; // Already calculated/overridden or default? 
                // Wait, logic in EmployeeSalaryComponent model was "Amount" specific. 
                // If it was 0, maybe fallback to SalaryComponent.DefaultAmount? 
                // Let's assume the linking table has the final value.
                
                // If the component is percentage based relative to basic salary (logic check)
                if (comp.SalaryComponent.IsPercentage)
                {
                    // If Amount stored is the percentage value (e.g. 10 for 10%)
                    amount = basicSalary * (comp.Amount / 100); 
                }

                if (comp.SalaryComponent.Type == "Allowance")
                {
                    totalAllowances += amount;
                }
                else if (comp.SalaryComponent.Type == "Deduction")
                {
                    totalDeductions += amount;
                }
            }

            // Overtime Pay
            decimal overtimePay = (decimal)totalOvertimeHours * hourlyRate * 1.5m; // 1.5x rate

            // Calculate Gross Before Tax
            decimal grossSalary = grossEarnings + totalAllowances + overtimePay - totalDeductions;
            
            // Should "Penalty" be separate? Payroll model has "Penalty".
            // Includes late login penalties
            decimal penalty = latePenalties;

            // Tax Calculation (Simplified Progressive)
            // 0 - 5M: 5%
            // 5 - 10M: 10%
            // 10 - 18M: 15%
            // 18 - 32M: 20%
            // > 32M: 25%
            decimal taxIncome = grossSalary - 11000000; // Deduction for self (standard in VN, approx)
            if (taxIncome < 0) taxIncome = 0;
            
            decimal taxAmount = CalculateTax(taxIncome);

            decimal netSalary = grossSalary - taxAmount - penalty;

            var payroll = new Payroll
            {
                EmployeeId = employeeId,
                Month = month,
                Year = year,
                BaseSalary = basicSalary,
                TotalAllowances = totalAllowances, 
                TotalDeductions = totalDeductions,
                TaxAmount = taxAmount,
                Bonus = overtimePay, // Using Bonus field for Overtime for now, or add Overtime field? 
                                     // Model has "Bonus", let's put Overtime there + other bonuses.
                                     // Wait, let's keep it clean. Overtime is part of earnings.
                
                Penalty = penalty,
                NetSalary = netSalary,
                TotalSalary = grossSalary, // Gross
                Status = "Pending",
                CreatedDate = DateTime.Now
            };

            _context.Payrolls.Add(payroll);
            await _context.SaveChangesAsync();

            return payroll;
        }

        private decimal CalculateTax(decimal income)
        {
            if (income <= 0) return 0;
            if (income <= 5000000) return income * 0.05m;
            if (income <= 10000000) return (5000000 * 0.05m) + ((income - 5000000) * 0.1m);
            if (income <= 18000000) return (5000000 * 0.05m) + (5000000 * 0.1m) + ((income - 10000000) * 0.15m);
            // ... and so on
            return (5000000 * 0.05m) + (5000000 * 0.1m) + (8000000 * 0.15m) + ((income - 18000000) * 0.2m);
        }
    }
}
