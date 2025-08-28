using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CybontrolX.DataBase;
using CybontrolX.DBModels;
using Microsoft.AspNetCore.Authorization;

namespace CybontrolX.Pages
{
    [Authorize(Roles = "Admin, Manager")]
    public class CreateClientModel : PageModel
    {
        private readonly AppDbContext _context;

        public CreateClientModel(AppDbContext context)
        {
            _context = context;
        }

        public string NotificationMessage { get; private set; } = string.Empty;

        [BindProperty]
        public Client NewClient { get; set; }

        public async Task<IActionResult> OnPostAddClientAsync()
        {
            if (!ModelState.IsValid)
            {
                NotificationMessage = "Ошибка: Некорректные данные.";
                return Page();
            }

            try
            {
                _context.Clients.Add(NewClient);
                await _context.SaveChangesAsync();
                return RedirectToPage("/Clients");
            }
            catch
            {
                NotificationMessage = "Ошибка: Клиент не был добавлен.";
            }

            return Page();
        }
    }
}