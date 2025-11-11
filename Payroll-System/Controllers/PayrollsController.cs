using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PayrollSystem.Web.Data;
using PayrollSystem.Web.Models;
using PayrollSystem.Web.Services; // <<-- for IPayrollService
using System;

namespace PayrollSystem.Web.Controllers
{
    [Authorize(Roles = "Admin")] // Only Admins can manage payroll
    public class PayrollsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPayrollService _payrollService;

        public PayrollsController(ApplicationDbContext context, IPayrollService payrollService)
        {
            _context = context;
            _payrollService = payrollService;
        }

        // GET: Payrolls
        public async Task<IActionResult> Index()
        {
            var payrolls = _context.Payrolls.Include(p => p.Employee);
            return View(await payrolls.ToListAsync());
        }

        // ---------- Existing CRUD actions remain unchanged ----------
        // Details, Create, Edit, Delete ... (keep your existing implementations)
        // (You can keep the earlier CRUD code; omitted here for brevity.)

        // -------------------- NEW: Run payroll UI --------------------

        // GET: Payrolls/Run
        public IActionResult Run()
        {
            // Provide a simple model for the form (year, month, overwrite checkbox)
            ViewData["Years"] = Enumerable.Range(DateTime.UtcNow.Year - 5, 11).Select(y => new SelectListItem(y.ToString(), y.ToString()));
            ViewData["Months"] = Enumerable.Range(1, 12).Select(m => new SelectListItem(
                new DateTime(2000, m, 1).ToString("MMMM"), m.ToString()));

            return View();
        }

        // POST: Payrolls/Run
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Run(int year, int month, bool overwrite = false)
        {
            var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // Process payroll using the service
            var results = await _payrollService.ProcessPayrollAsync(year, month, userId ?? "system", overwrite);

            // Pass results to RunResult view
            return View("RunResult", results);
        }

        // GET: Payrolls/RunResult (optional direct link)
        public async Task<IActionResult> RunResult(int year, int month)
        {
            var results = await _payrollService.GetPayrollForMonthAsync(year, month);
            return View(results);
        }

        // ---------- Keep your PayrollExists helper ----------
        private bool PayrollExists(int id)
        {
            return _context.Payrolls.Any(e => e.Id == id);
        }
    }
}
