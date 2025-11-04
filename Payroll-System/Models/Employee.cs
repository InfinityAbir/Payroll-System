using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace PayrollSystem.Web.Models
{
    public class Employee
    {
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        public string Designation { get; set; }

        [Required]
        public decimal BasicSalary { get; set; }

        [DataType(DataType.Date)]
        public DateTime JoiningDate { get; set; }

        public string? UserId { get; set; } // Link to Identity user

        // Navigation properties
        public ICollection<Attendance> Attendances { get; set; }
        public ICollection<Payroll> Payrolls { get; set; }
    }
}
