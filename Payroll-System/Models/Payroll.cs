using System;
using System.ComponentModel.DataAnnotations;

namespace PayrollSystem.Web.Models
{
    public class Payroll
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }

        [DataType(DataType.Date)]
        public DateTime SalaryMonth { get; set; }

        public decimal BasicSalary { get; set; }
        public decimal Allowances { get; set; }
        public decimal Deductions { get; set; }

        public decimal NetSalary => BasicSalary + Allowances - Deductions;

        // Navigation property
        public Employee Employee { get; set; }
    }
}
