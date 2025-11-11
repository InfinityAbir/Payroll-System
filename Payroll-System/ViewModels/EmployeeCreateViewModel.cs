using System;
using System.ComponentModel.DataAnnotations;

namespace PayrollSystem.Web.ViewModels
{
    public class EmployeeCreateViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        public string Designation { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        [Display(Name = "Basic Salary")]
        public decimal BasicSalary { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Joining Date")]
        public DateTime JoiningDate { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; }
    }
}
