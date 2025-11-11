// PayrollSystem.Web/Data/SeedRoles.cs
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace PayrollSystem.Web.Data
{
    public static class SeedRoles
    {
        private static readonly string[] Roles = new[] { "Admin", "Manager", "Employee" };

        public static async Task InitializeRoles(RoleManager<IdentityRole> roleManager)
        {
            foreach (var role in Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }
    }
}
