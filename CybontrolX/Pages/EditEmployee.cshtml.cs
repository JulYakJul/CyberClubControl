using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using CybontrolX.DataBase;
using CybontrolX.DBModels;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace CybontrolX.Pages
{
    [Authorize(Roles = "Manager")]
    public class EditEmployeeModel : PageModel
    {
        private readonly AppDbContext _context;

        public EditEmployeeModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Employee Employee { get; set; }
        public IList<DutySchedule> DutySchedules { get; set; }
        public string NotificationMessage { get; private set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Employee = await _context.Employees.FindAsync(id);
            if (Employee == null)
            {
                return NotFound();
            }

            DutySchedules = await _context.DutySchedules.ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostEditEmployeeAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var employeeInDb = await _context.Employees.FindAsync(Employee.Id);
            if (employeeInDb == null)
            {
                return NotFound();
            }

            employeeInDb.Name = Employee.Name;
            employeeInDb.Surname = Employee.Surname;
            employeeInDb.Patronymic = Employee.Patronymic;
            employeeInDb.PhoneNumber = Employee.PhoneNumber;
            employeeInDb.Status = Employee.Status;
            employeeInDb.Role = Employee.Role;
            employeeInDb.DutyScheduleId = Employee.DutyScheduleId;
            employeeInDb.ShiftStart = Employee.ShiftStart;
            employeeInDb.ShiftEnd = Employee.ShiftEnd;

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToPage("ClubStaff");
            }
            catch
            {
                NotificationMessage = "Ошибка: Не удалось сохранить изменения.";
                return Page();
            }
        }
    }
}
