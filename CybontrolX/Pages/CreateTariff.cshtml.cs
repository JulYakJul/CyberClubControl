using CybontrolX.DataBase;
using CybontrolX.DBModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;

namespace CybontrolX.Pages
{
    [Authorize(Roles = "Admin, Manager")]
    public class CreateTariffModel : PageModel
    {
        private readonly AppDbContext _context;

        public CreateTariffModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Name { get; set; }

        [BindProperty]
        public double SessionTime { get; set; }

        [BindProperty]
        public List<string> Days { get; set; }

        [BindProperty]
        public double Price { get; set; }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var daysOfWeek = Days?.Select(day =>
            {
                return day switch
                {
                    "Monday" => DayOfWeek.Monday,
                    "Tuesday" => DayOfWeek.Tuesday,
                    "Wednesday" => DayOfWeek.Wednesday,
                    "Thursday" => DayOfWeek.Thursday,
                    "Friday" => DayOfWeek.Friday,
                    "Saturday" => DayOfWeek.Saturday,
                    "Sunday" => DayOfWeek.Sunday,
                    _ => throw new ArgumentException("Неизвестный день недели")
                };
            }).ToList() ?? new List<DayOfWeek>();

            var tariff = new Tariff
            {
                Name = Name,
                SessionTime = TimeSpan.FromHours(SessionTime),
                Days = daysOfWeek,
                Price = Price
            };

            _context.Tariffs.Add(tariff);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Tariffs");
        }

        private string FormatDays(List<DayOfWeek> daysOfWeek)
        {
            if (daysOfWeek == null || daysOfWeek.Count == 0)
            {
                return string.Empty;
            }

            var weekDays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };

            var sortedDays = daysOfWeek.OrderBy(day => Array.IndexOf(weekDays, day)).ToList();

            bool AreConsecutive(List<DayOfWeek> daysList)
            {
                for (int i = 0; i < daysList.Count - 1; i++)
                {
                    int currentIndex = Array.IndexOf(weekDays, daysList[i]);
                    int nextIndex = Array.IndexOf(weekDays, daysList[i + 1]);
                    if (nextIndex != currentIndex + 1)
                    {
                        return false;
                    }
                }
                return true;
            }

            if (AreConsecutive(sortedDays))
            {
                return $"{sortedDays.First().ToString().Substring(0, 2)}-{sortedDays.Last().ToString().Substring(0, 2)}";
            }
            else
            {
                return string.Join(", ", sortedDays.Select(day => day.ToString().Substring(0, 2)));
            }
        }
    }
}