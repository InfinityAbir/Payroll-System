using System;
using System.ComponentModel.DataAnnotations;

namespace PayrollSystem.Web.Models
{
    public class Attendance
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }

        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        public bool IsPresent { get; set; }

        // Navigation property
        public Employee Employee { get; set; }
    }
}
