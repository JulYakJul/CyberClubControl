using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CybontrolX.DataBase;
using CybontrolX.DBModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace CybontrolX.Pages
{
    [Authorize(Roles = "Manager")]
    public class DeleteDutyCalendarModel : PageModel
    {
        private readonly AppDbContext _context;

        public DeleteDutyCalendarModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public int EmployeeId { get; set; }

        public List<Employee> Employees { get; set; }

        public async Task OnGetAsync()
        {
            Employees = await _context.Employees
                .Where(e => _context.DutySchedules.Any(ds => ds.EmployeeId == e.Id))
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (EmployeeId == 0)
            {
                ModelState.AddModelError("EmployeeId", "Сотрудник не выбран.");
                return Page();
            }

            var schedulesToDelete = await _context.DutySchedules
                .Where(ds => ds.EmployeeId == EmployeeId)
                .ToListAsync();

            if (schedulesToDelete.Any())
            {
                _context.DutySchedules.RemoveRange(schedulesToDelete);
                await _context.SaveChangesAsync();
                ViewData["Message"] = "График дежурств успешно удален.";
            }
            else
            {
                ViewData["Message"] = "Нет записей для удаления.";
            }

            return RedirectToPage("/DutyCalendar");
        }
    }
}