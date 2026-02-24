using System;
using System.Collections.Generic;

namespace Sales_Management.Models;

public partial class Employee
{
    public int EmployeeId { get; set; }

    public int UserId { get; set; }

    public string? Position { get; set; }

    public decimal? BasicSalary { get; set; }

    public DateOnly? StartWorkingDate { get; set; }

    public string? Department { get; set; }

    public int? ShiftId { get; set; }
    
    public string? ContractType { get; set; } // Full-time, Part-time, Intern

    public string? ContractFile { get; set; } // Path to uploaded contract

    public bool IsDeleted { get; set; }

    public string? ChangeHistory { get; set; } // JSON or text summary of changes

    public virtual User User { get; set; } = null!;

    public virtual Shift? Shift { get; set; }

    public virtual ICollection<TimeAttendance> TimeAttendances { get; set; } = new List<TimeAttendance>();

    public virtual ICollection<Payroll> Payrolls { get; set; } = new List<Payroll>();
}
