using CybontrolX.DataBase;
using CybontrolX.DBModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CybontrolX.Pages
{
    [Authorize(Roles = "Admin, Manager")]
    public class EditTariffModel : PageModel
    {
        private readonly AppDbContext _context;

        public EditTariffModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Tariff Tariff { get; set; }

        [BindProperty]
        public double SessionTime { get; set; }

        [BindProperty]
        public List<string> SelectedDays { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Tariff = await _context.Tariffs.FindAsync(id);
            if (Tariff == null) return NotFound();

            SessionTime = Tariff.SessionTime.TotalHours;
            SelectedDays = Tariff.Days.Select(d => d.ToString()).ToList();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var existingTariff = await _context.Tariffs.FindAsync(Tariff.Id);
            if (existingTariff == null) return NotFound();

            existingTariff.Name = Tariff.Name;
            existingTariff.SessionTime = TimeSpan.FromHours(SessionTime);
            existingTariff.Days = SelectedDays.Select(day => Enum.Parse<DayOfWeek>(day)).ToList();
            existingTariff.Price = Tariff.Price;

            await _context.SaveChangesAsync();
            return RedirectToPage("/Tariffs");
        }
    }
}
