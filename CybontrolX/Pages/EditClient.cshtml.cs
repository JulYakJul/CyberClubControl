using CybontrolX.DataBase;
using CybontrolX.DBModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CybontrolX.Pages
{
    [Authorize(Roles = "Admin, Manager")]
    public class EditClientModel : PageModel
    {
        private readonly AppDbContext _context;

        public EditClientModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Client EditClient { get; set; }

        public IActionResult OnGet(int id)
        {
            EditClient = _context.Clients.FirstOrDefault(c => c.Id == id);

            if (EditClient == null)
            {
                return NotFound();
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var clientInDb = _context.Clients.FirstOrDefault(c => c.Id == EditClient.Id);

            if (clientInDb == null)
            {
                return NotFound();
            }

            clientInDb.Name = EditClient.Name;
            clientInDb.PhoneNumber = EditClient.PhoneNumber;

            _context.SaveChanges();

            return RedirectToPage("/Clients");
        }
    }
}