using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CybontrolX.DataBase;
using CybontrolX.DBModels;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace CybontrolX.Pages
{
    [Authorize(Roles = "Admin, Manager")]
    public class DutyCalendarModel : PageModel
    {
        private readonly AppDbContext _context;

        public DutyCalendarModel(AppDbContext context)
        {
            _context = context;
        }

        public string EventsJson { get; set; }

        public async Task OnGetAsync()
        {
            var schedules = await (from ds in _context.DutySchedules
                                   join emp in _context.Employees on ds.EmployeeId equals emp.Id
                                   select new
                                   {
                                       EmployeeSurname = emp.Surname,
                                       ds.DutyDate,
                                       ds.ShiftStart,
                                       ds.ShiftEnd
                                   }).ToListAsync();

            var events = new List<object>();

            foreach (var item in schedules)
            {
                var employeeName = item.EmployeeSurname ?? "Неизвестный";

                //string color = item.ShiftType switch
                //{
                //    ShiftType.Day => "#00FF00",    // зелёный для дневной смены
                //    ShiftType.Night => "#a003f9",  // фиолетовый для ночной смены
                //    ShiftType.Other => "#FFA500",  // оранжевый для прочих смен
                //    _ => "#000000"                 
                //};

                // Если смена начинается до полуночи, а заканчивается после
                if (item.ShiftStart > item.ShiftEnd)
                {
                    // Первая часть смены (до полуночи)
                    events.Add(new
                    {
                        title = $"{employeeName} - {item.ShiftStart:hh\\:mm} - 23:59",
                        start = item.DutyDate.Add(item.ShiftStart).ToString("yyyy-MM-ddTHH:mm:ss"),
                        end = item.DutyDate.Add(new TimeSpan(23, 59, 59)).ToString("yyyy-MM-ddTHH:mm:ss")
                    });

                    // Вторая часть смены (после полуночи)
                    events.Add(new
                    {
                        title = $"{employeeName} - 00:00 - {item.ShiftEnd:hh\\:mm}",
                        start = item.DutyDate.AddDays(1).ToString("yyyy-MM-ddT00:00:00"),
                        end = item.DutyDate.AddDays(1).Add(item.ShiftEnd).ToString("yyyy-MM-ddTHH:mm:ss")
                    });
                }
                else
                {
                    events.Add(new
                    {
                        title = $"{employeeName} - {item.ShiftStart:hh\\:mm} - {item.ShiftEnd:hh\\:mm}",
                        start = item.DutyDate.Add(item.ShiftStart).ToString("yyyy-MM-ddTHH:mm:ss"),
                        end = item.DutyDate.Add(item.ShiftEnd).ToString("yyyy-MM-ddTHH:mm:ss")
                    });
                }
            }

            EventsJson = JsonSerializer.Serialize(events);
        }

        public bool IsManager()
        {
            return User.IsInRole("Manager");
        }
    }
}
