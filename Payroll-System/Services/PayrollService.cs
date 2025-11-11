using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PayrollSystem.Web.Data;
using PayrollSystem.Web.Models;

namespace PayrollSystem.Web.Services
{
    public interface IPayrollService
    {
        /// <summary>
        /// Process payroll for all employees for the given year/month.
        /// If overwrite == true, existing payroll records for that month will be removed and recalculated.
        /// Returns the list of created payroll records.
        /// </summary>
        Task<List<Payroll>> ProcessPayrollAsync(int year, int month, string runByUserId, bool overwrite = false);

        /// <summary>
        /// Get payroll records for a month (year, month).
        /// </summary>
        Task<List<Payroll>> GetPayrollForMonthAsync(int year, int month);
    }

    public class PayrollService : IPayrollService
    {
        private readonly ApplicationDbContext _db;

        // ----- Configurable rules -----
        // Tax percent applied on gross salary
        private readonly decimal _taxPercent = 10m;

        // A simple per-day attendance allowance (for example: transport/meal)
        // You can change this to a percentage model if you prefer.
        private readonly decimal _allowancePerPresentDay = 50m;

        // Minimum working days considered in a month for full salary proration calculation.
        // We compute actual working days (weekdays) for the month, but this floor prevents divide-by-zero.
        private const int MinimumWorkingDaysFloor = 1;

        public PayrollService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<Payroll>> GetPayrollForMonthAsync(int year, int month)
        {
            var monthStart = new DateTime(year, month, 1);
            return await _db.Payrolls
                .Include(p => p.Employee)
                .Where(p => p.SalaryMonth.Year == year && p.SalaryMonth.Month == month)
                .ToListAsync();
        }

        public async Task<List<Payroll>> ProcessPayrollAsync(int year, int month, string runByUserId, bool overwrite = false)
        {
            if (year < 2000 || month < 1 || month > 12)
                throw new ArgumentException("Invalid year or month.");

            var salaryMonth = new DateTime(year, month, 1);

            // If overwrite requested, delete existing payrolls for the month first
            if (overwrite)
            {
                var existing = await _db.Payrolls
                    .Where(p => p.SalaryMonth.Year == year && p.SalaryMonth.Month == month)
                    .ToListAsync();
                if (existing.Any())
                {
                    _db.Payrolls.RemoveRange(existing);
                    await _db.SaveChangesAsync();
                }
            }
            else
            {
                // If any payroll already exists and not overwriting, return existing records (avoid double-processing)
                var existing = await _db.Payrolls
                    .Include(p => p.Employee)
                    .Where(p => p.SalaryMonth.Year == year && p.SalaryMonth.Month == month)
                    .ToListAsync();
                if (existing.Any())
                    return existing;
            }

            // Load employees and their attendance for the month
            var monthStart = salaryMonth;
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var employees = await _db.Employees
                .Include(e => e.Attendances.Where(a => a.Date >= monthStart && a.Date <= monthEnd))
                .ToListAsync();

            var workingDaysInMonth = CountWeekDaysInMonth(year, month);
            if (workingDaysInMonth < MinimumWorkingDaysFloor) workingDaysInMonth = MinimumWorkingDaysFloor;

            var results = new List<Payroll>();

            foreach (var emp in employees)
            {
                // Count present days from attendance. If there are no attendance records, we assume full attendance.
                var attendanceRecords = emp.Attendances ?? new List<Attendance>();
                int presentDays;
                if (attendanceRecords.Any())
                {
                    presentDays = attendanceRecords.Count(a => a.IsPresent);
                }
                else
                {
                    // No attendance recorded => assume full present for working days (you can change policy)
                    presentDays = workingDaysInMonth;
                }

                // Prorate basic salary by attendance: (presentDays / workingDays) * BasicSalary
                decimal proratedBasic = 0m;
                if (workingDaysInMonth > 0)
                {
                    proratedBasic = Math.Round(emp.BasicSalary * ((decimal)presentDays / (decimal)workingDaysInMonth), 2);
                }

                // Allowances — simple per-present-day allowance
                decimal allowances = Math.Round(_allowancePerPresentDay * presentDays, 2);

                // Gross = prorated basic + allowances
                decimal gross = Math.Round(proratedBasic + allowances, 2);

                // Deductions — simple tax percent over gross (you can extend to include loans, advances, provident, etc.)
                decimal tax = Math.Round(gross * (_taxPercent / 100m), 2);
                decimal deductions = tax; // extend by adding other deduction types

                decimal net = Math.Round(gross - deductions, 2);

                var payroll = new Payroll
                {
                    EmployeeId = emp.Id,
                    SalaryMonth = salaryMonth,
                    BasicSalary = proratedBasic,
                    Allowances = allowances,
                    Deductions = deductions,
                    // NetSalary is computed property in model, but we store fields too.
                    Employee = emp
                };

                // Add record (do not call Save per-employee; batch save later)
                results.Add(payroll);
            }

            if (results.Any())
            {
                await _db.Payrolls.AddRangeAsync(results);
                await _db.SaveChangesAsync();
            }

            // Reload created payrolls with Employee nav property included
            var created = await _db.Payrolls
                .Include(p => p.Employee)
                .Where(p => p.SalaryMonth.Year == year && p.SalaryMonth.Month == month)
                .ToListAsync();

            return created;
        }

        /// <summary>
        /// Count weekdays (Mon-Fri) in a month.
        /// </summary>
        private static int CountWeekDaysInMonth(int year, int month)
        {
            var start = new DateTime(year, month, 1);
            var end = start.AddMonths(1).AddDays(-1);

            int count = 0;
            for (var day = start; day <= end; day = day.AddDays(1))
            {
                if (day.DayOfWeek != DayOfWeek.Saturday && day.DayOfWeek != DayOfWeek.Sunday)
                    count++;
            }

            return count;
        }
    }
}
