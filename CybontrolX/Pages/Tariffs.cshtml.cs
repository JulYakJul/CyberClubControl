using CybontrolX.DataBase;
using CybontrolX.DBModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CybontrolX.Pages
{
    [Authorize(Roles = "Admin, Manager")]
    public class TariffsModel : PageModel
    {
        private readonly AppDbContext _context;

        public TariffsModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Tariff> Tariffs { get; set; }

        public async Task OnGet(string searchTerm)
        {
            if (!string.IsNullOrEmpty(searchTerm))
            {
                Tariffs = await _context.Tariffs
                    .Where(t => t.Name.ToLower().Contains(searchTerm.ToLower()))
                    .ToListAsync();
            }
            else
            {
                Tariffs = await _context.Tariffs.ToListAsync();
            }
        }

        public IActionResult OnGetSearch(string searchTerm)
        {
            if (!string.IsNullOrEmpty(searchTerm))
            {
                var tariffs = _context.Tariffs
                    .Where(t => t.Name.ToLower().Contains(searchTerm.ToLower()))
                    .ToList();

                return Partial("_TariffsPartial", tariffs);
            }

            var allTariffs = _context.Tariffs.ToList();
            return Partial("_TariffsPartial", allTariffs);
        }

        public async Task<IActionResult> OnPostDeleteAsync(int tariffId)
        {
            var tariff = await _context.Tariffs.FindAsync(tariffId);
            if (tariff != null)
            {
                _context.Tariffs.Remove(tariff);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public string FormatDays(List<DayOfWeek> daysOfWeek)
        {
            if (daysOfWeek == null || daysOfWeek.Count == 0)
            {
                return string.Empty;
            }

            var weekDays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };
            var sortedDays = daysOfWeek.OrderBy(day => Array.IndexOf(weekDays, day)).ToList();

            string GetRussianDayName(DayOfWeek day) =>
                day switch
                {
                    DayOfWeek.Monday => "Ïí",
                    DayOfWeek.Tuesday => "Âò",
                    DayOfWeek.Wednesday => "Ñð",
                    DayOfWeek.Thursday => "×ò",
                    DayOfWeek.Friday => "Ïò",
                    DayOfWeek.Saturday => "Ñá",
                    DayOfWeek.Sunday => "Âñ",
                    _ => string.Empty
                };

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

            return AreConsecutive(sortedDays)
                ? $"{GetRussianDayName(sortedDays.First())}-{GetRussianDayName(sortedDays.Last())}"
                : string.Join(", ", sortedDays.Select(GetRussianDayName));
        }
    }
}