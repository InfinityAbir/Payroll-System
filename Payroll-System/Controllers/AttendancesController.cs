using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection; // for [ActivatorUtilitiesConstructor]
using PayrollSystem.Web.Data;
using PayrollSystem.Web.Models;

namespace PayrollSystem.Web.Controllers
{
    [Authorize] // must be signed in
    public class AttendancesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        // Make THIS the only applicable constructor for DI
        [ActivatorUtilitiesConstructor]
        public AttendancesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ========= EMPLOYEE VIEW =========
        // GET: /Attendances/My
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> My()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var records = await _context.Attendances
                .Include(a => a.Employee)
                .Where(a => a.Employee.UserId == user.Id)
                .OrderByDescending(a => a.Date)
                .ToListAsync();

            return View(records);
        }

        // ========= ADMIN MANAGEMENT =========
        // GET: /Attendances
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var attendances = await _context.Attendances
                .Include(a => a.Employee)
                .OrderByDescending(a => a.Date)
                .ToListAsync();

            return View(attendances);
        }

        // GET: /Attendances/Details/5
        // Admins can see any; employees only their own.
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var attendance = await _context.Attendances
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attendance == null) return NotFound();

            if (!User.IsInRole("Admin"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (attendance.Employee?.UserId != user?.Id)
                    return Forbid();
            }

            return View(attendance);
        }

        // GET: /Attendances/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["EmployeeId"] = new SelectList(_context.Employees, "Id", "FullName");
            return View();
        }

        // POST: /Attendances/Create
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EmployeeId,Date,IsPresent")] Attendance attendance)
        {
            if (!ModelState.IsValid)
            {
                ViewData["EmployeeId"] = new SelectList(_context.Employees, "Id", "FullName", attendance.EmployeeId);
                return View(attendance);
            }

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /Attendances/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance == null) return NotFound();

            ViewData["EmployeeId"] = new SelectList(_context.Employees, "Id", "FullName", attendance.EmployeeId);
            return View(attendance);
        }

        // POST: /Attendances/Edit/5
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,EmployeeId,Date,IsPresent")] Attendance attendance)
        {
            if (id != attendance.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewData["EmployeeId"] = new SelectList(_context.Employees, "Id", "FullName", attendance.EmployeeId);
                return View(attendance);
            }

            try
            {
                _context.Update(attendance);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AttendanceExists(attendance.Id)) return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Attendances/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var attendance = await _context.Attendances
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attendance == null) return NotFound();

            return View(attendance);
        }

        // POST: /Attendances/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance != null)
            {
                _context.Attendances.Remove(attendance);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool AttendanceExists(int id) => _context.Attendances.Any(a => a.Id == id);
    }
}
