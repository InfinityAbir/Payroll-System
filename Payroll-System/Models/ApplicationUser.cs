using Microsoft.AspNetCore.Identity;
using System;

namespace PayrollSystem.Web.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string? Designation { get; set; }
        public DateTime? JoiningDate { get; set; }
        public decimal? BasicSalary { get; set; }
    }
}
