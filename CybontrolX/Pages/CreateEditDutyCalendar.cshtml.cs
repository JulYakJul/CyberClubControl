using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CybontrolX.DataBase;
using CybontrolX.DBModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace CybontrolX.Pages
{
    [Authorize(Roles = "Manager")]
    public class CreateEditDutyCalendarModel : PageModel
    {
        private readonly AppDbContext _context;

        public CreateEditDutyCalendarModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public int EmployeeId { get; set; }

        [BindProperty]
        public string DutyDates { get; set; }

        [BindProperty]
        public TimeSpan ShiftStart { get; set; }

        [BindProperty]
        public TimeSpan ShiftEnd { get; set; }

        [BindProperty]
        public string WarningMessage { get; set; }

        public List<Employee> Employees { get; set; }

        [BindProperty]
        public ShiftType SelectedShiftType { get; set; } = ShiftType.Day;

        public async Task<IActionResult> OnGetAsync(int employeeId = 0, string shiftType = null)
        {
            Employees = await _context.Employees
                .Where(e => e.DeletedAt == null)
                .ToListAsync();

            if (!string.IsNullOrEmpty(shiftType))
            {
                if (Enum.TryParse<ShiftType>(shiftType, true, out var parsedShiftType))
                {
                    SelectedShiftType = parsedShiftType;
                    SetShiftTimesByType(SelectedShiftType);
                }
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var query = _context.DutySchedules
                    .Where(ds => ds.EmployeeId == employeeId);

                var dutySchedules = await query.ToListAsync();

                var dutyDates = dutySchedules
                    .Select(ds => ds.DutyDate.ToString("yyyy-MM-dd"))
                    .Distinct()
                    .ToList();

                return new JsonResult(new { dutyDates = string.Join(",", dutyDates) });
            }

            var dutySchedulesForPage = await _context.DutySchedules
                .Where(ds => ds.EmployeeId == employeeId)
                .ToListAsync();

            DutyDates = string.Join(",", dutySchedulesForPage
                .Select(ds => ds.DutyDate.ToString("yyyy-MM-dd")));

            var firstSchedule = dutySchedulesForPage.FirstOrDefault();
            if (firstSchedule != null)
            {
                ShiftStart = firstSchedule.ShiftStart;
                ShiftEnd = firstSchedule.ShiftEnd;
            }

            return Page();
        }

        private void SetShiftTimesByType(ShiftType shiftType)
        {
            switch (shiftType)
            {
                case ShiftType.Night:
                    ShiftStart = new TimeSpan(21, 0, 0);
                    ShiftEnd = new TimeSpan(8, 0, 0);
                    break;
                case ShiftType.Day:
                    ShiftStart = new TimeSpan(8, 0, 0);
                    ShiftEnd = new TimeSpan(21, 0, 0);
                    break;
                case ShiftType.Other:
                    ShiftStart = new TimeSpan(10, 0, 0);
                    ShiftEnd = new TimeSpan(22, 0, 0);
                    break;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            SetShiftTimesByType(SelectedShiftType);

            if (EmployeeId == 0)
            {
                ModelState.AddModelError("EmployeeId", "Сотрудник не выбран.");
                Employees = await _context.Employees
                    .Where(e => e.DeletedAt == null)
                    .ToListAsync();
                return Page();
            }

            var dates = DutyDates.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(d => DateTime.ParseExact(d.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture))
                .OrderBy(d => d)
                .ToList();

            if (HasMoreThan3ConsecutiveDays(dates))
            {
                WarningMessage = "Нельзя назначать более 3 смен подряд без выходного дня.";
                Employees = await _context.Employees
                    .Where(e => e.DeletedAt == null)
                    .ToListAsync();
                return Page();
            }

            // Удаляем только смены выбранного типа
            var existingSchedules = await _context.DutySchedules
                .Where(ds => ds.EmployeeId == EmployeeId && ds.ShiftType == SelectedShiftType)
                .ToListAsync();

            _context.DutySchedules.RemoveRange(existingSchedules);
            await _context.SaveChangesAsync();

            var schedules = dates.Select(date => new DutySchedule
            {
                EmployeeId = EmployeeId,
                DutyDate = DateTime.SpecifyKind(date, DateTimeKind.Utc),
                ShiftStart = ShiftStart,
                ShiftEnd = ShiftEnd,
                ShiftType = SelectedShiftType
            }).ToList();

            _context.DutySchedules.AddRange(schedules);
            await _context.SaveChangesAsync();

            return RedirectToPage("/DutyCalendar");
        }

        public async Task<IActionResult> OnGetGetDutyDatesByShiftTypeAsync(string shiftType, int? employeeId)
        {
            if (!Enum.TryParse<ShiftType>(shiftType, true, out var parsedShiftType) || !employeeId.HasValue)
                return new JsonResult(new { dutyDates = "" });

            var dates = await _context.DutySchedules
                .Where(ds => ds.EmployeeId == employeeId.Value &&
                             ds.ShiftType == parsedShiftType)
                .Select(ds => ds.DutyDate.ToString("yyyy-MM-dd"))
                .Distinct()
                .ToListAsync();

            return new JsonResult(new { dutyDates = string.Join(",", dates) });
        }

        private bool HasMoreThan3ConsecutiveDays(List<DateTime> dates)
        {
            if (dates.Count == 0) return false;

            int consecutiveCount = 1;

            for (int i = 1; i < dates.Count; i++)
            {
                if ((dates[i] - dates[i - 1]).Days == 1)
                {
                    consecutiveCount++;
                    if (consecutiveCount > 3)
                        return true;
                }
                else
                {
                    consecutiveCount = 1;
                }
            }

            return false;
        }
    }
}