using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CybontrolX.DataBase;
using CybontrolX.DBModels;
using Microsoft.AspNetCore.Authorization;

namespace CybontrolX.Pages
{
    [Authorize(Roles = "Manager")]
    public class ClubStaffModel : PageModel
    {
        private readonly AppDbContext _context;

        public ClubStaffModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<Employee> Employees { get; set; } = new List<Employee>();

        [BindProperty]
        public List<int> SelectedEmployeeIds { get; set; } = new List<int>();

        [BindProperty(SupportsGet = true)]
        public string SearchQuery { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SortColumn { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool SortDescending { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.Employees
                .Where(e => e.DeletedAt == null)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                query = query.Where(e => e.Surname.Contains(SearchQuery));
            }

            var today = DateTime.UtcNow.Date;

            var dutySchedulesToday = await _context.DutySchedules
                .Where(ds => ds.DutyDate.Date == today)
                .ToListAsync();

            var employees = await query.ToListAsync();

            foreach (var employee in employees)
            {
                var employeeDutySchedule = dutySchedulesToday
                    .FirstOrDefault(ds => ds.EmployeeId == employee.Id);

                if (employeeDutySchedule != null)
                {
                    employee.Status = "Работает";
                }
                else
                {
                    employee.Status = "Не работает";
                }
            }

            Employees = SortColumn switch
            {
                "Name" => SortDescending ? employees.OrderByDescending(e => e.Name).ToList() : employees.OrderBy(e => e.Name).ToList(),
                "Surname" => SortDescending ? employees.OrderByDescending(e => e.Surname).ToList() : employees.OrderBy(e => e.Surname).ToList(),
                "Patronymic" => SortDescending ? employees.OrderByDescending(e => e.Patronymic).ToList() : employees.OrderBy(e => e.Patronymic).ToList(),
                "PhoneNumber" => SortDescending ? employees.OrderByDescending(e => e.PhoneNumber).ToList() : employees.OrderBy(e => e.PhoneNumber).ToList(),
                "Status" => SortDescending ? employees.OrderByDescending(e => e.Status).ToList() : employees.OrderBy(e => e.Status).ToList(),
                "Role" => SortDescending ? employees.OrderByDescending(e => e.Role).ToList() : employees.OrderBy(e => e.Role).ToList(),
                _ => employees.OrderBy(e => e.Surname).ToList(),
            };
        }

        public async Task<IActionResult> OnPostDeleteEmployeesAsync()
        {
            if (!string.IsNullOrWhiteSpace(Request.Form["SelectedEmployeeIds"]))
            {
                var selectedIds = Request.Form["SelectedEmployeeIds"]
                    .ToString()
                    .Split(',')
                    .Where(id => int.TryParse(id, out _))
                    .Select(int.Parse)
                    .ToList();

                if (selectedIds.Any())
                {
                    var employeesToDelete = await _context.Employees
                        .Where(e => selectedIds.Contains(e.Id))
                        .ToListAsync();

                    if (employeesToDelete.Any())
                    {
                        foreach (var employee in employeesToDelete)
                        {
                            employee.DeletedAt = DateTime.UtcNow;
                        }

                        await _context.SaveChangesAsync();
                    }
                }
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetAllEmployees()
        {
            var employees = await _context.Employees
                .Where(e => e.DeletedAt == null)
                .Select(e => new
                {
                    id = e.Id,
                    name = e.Name,
                    surname = e.Surname,
                    patronymic = e.Patronymic,
                    phoneNumber = e.PhoneNumber,
                    status = e.Status,
                    role = e.Role == Role.Manager ? "Управляющий" : "Администратор"
                })
                .ToListAsync();

            return new JsonResult(employees);
        }
    }
}
