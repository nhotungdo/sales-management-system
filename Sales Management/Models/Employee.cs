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

    public virtual User User { get; set; } = null!;
}
