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
    [Authorize(Roles = "Admin, Manager")]
    public class ClientsModel : PageModel
    {
        private readonly AppDbContext _context;

        public ClientsModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<Client> Clients { get; set; } = new List<Client>();

        [BindProperty]
        public List<int> SelectedClientIds { get; set; } = new List<int>();

        [BindProperty(SupportsGet = true)]
        public string SearchQuery { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SortColumn { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool SortDescending { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.Clients
                .Where(c => c.DeletedAt == null)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                query = query.Where(c => c.Name.Contains(SearchQuery) || c.PhoneNumber.Contains(SearchQuery));
            }

            query = SortColumn switch
            {
                "FullName" => SortDescending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
                "PhoneNumber" => SortDescending ? query.OrderByDescending(c => c.PhoneNumber) : query.OrderBy(c => c.PhoneNumber),
                _ => query.OrderBy(c => c.Name),
            };

            Clients = await query.ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteClientsAsync()
        {
            if (!string.IsNullOrWhiteSpace(Request.Form["SelectedClientIds"]))
            {
                var selectedIds = Request.Form["SelectedClientIds"]
                    .ToString()
                    .Split(',')
                    .Where(id => int.TryParse(id, out _))
                    .Select(int.Parse)
                    .ToList();

                if (selectedIds.Any())
                {
                    var clientsToDelete = await _context.Clients
                        .Where(c => selectedIds.Contains(c.Id))
                        .ToListAsync();

                    foreach (var client in clientsToDelete)
                    {
                        client.DeletedAt = DateTime.UtcNow;
                    }

                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetAllClients()
        {
            var clients = await _context.Clients
                .Where(c => c.DeletedAt == null)
                .Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    phoneNumber = c.PhoneNumber
                })
                .ToListAsync();

            return new JsonResult(clients);
        }
    }
}