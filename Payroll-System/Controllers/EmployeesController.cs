using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollSystem.Web.Data;
using PayrollSystem.Web.Models;
using PayrollSystem.Web.ViewModels;

namespace PayrollSystem.Web.Controllers
{
    [Authorize] // all actions require login
    public class EmployeesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public EmployeesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // List everyone (any authenticated user)
        public async Task<IActionResult> Index()
        {
            var list = await _context.Employees.ToListAsync();
            return View(list);
        }

        // Details (any authenticated user)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id);
            if (employee == null) return NotFound();

            return View(employee);
        }

        // Create (Admin only)
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View(new EmployeeCreateViewModel());
        }

        // Create (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var baseName = model.FullName.Replace(" ", "").ToLower();
            var userName = baseName;
            var email = $"{baseName}@company.com";

            int counter = 1;
            while (await _userManager.FindByNameAsync(userName) != null)
            {
                userName = $"{baseName}{counter}";
                email = $"{userName}@company.com";
                counter++;
            }

            var user = new ApplicationUser
            {
                UserName = userName,
                Email = email,
                FullName = model.FullName,
                Designation = model.Designation,
                JoiningDate = model.JoiningDate,
                BasicSalary = model.BasicSalary
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return View(model);
            }

            if (!await _roleManager.RoleExistsAsync("Employee"))
                await _roleManager.CreateAsync(new IdentityRole("Employee"));

            await _userManager.AddToRoleAsync(user, "Employee");

            var employee = new Employee
            {
                FullName = model.FullName,
                Designation = model.Designation,
                BasicSalary = model.BasicSalary,
                JoiningDate = model.JoiningDate,
                UserId = user.Id
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Edit (Admin only)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            return View(employee);
        }

        // Edit (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Employee employee)
        {
            if (id != employee.Id) return NotFound();

            if (!ModelState.IsValid)
                return View(employee);

            try
            {
                _context.Update(employee);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Employees.Any(e => e.Id == employee.Id))
                    return NotFound();

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // Delete (Admin only)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id);
            if (employee == null) return NotFound();

            return View(employee);
        }

        // Delete (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                var user = await _userManager.FindByIdAsync(employee.UserId);
                if (user != null)
                    await _userManager.DeleteAsync(user);

                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
