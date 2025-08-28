using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CybontrolX.DataBase;
using CybontrolX.DBModels;
using Microsoft.AspNetCore.Authorization;

namespace CybontrolX.Pages
{
    [Authorize(Roles = "Admin, Manager")]
    public class ComputersModel : PageModel
    {
        private readonly AppDbContext _context;

        public ComputersModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<Computer> Computers { get; set; } = new List<Computer>();

        [BindProperty]
        public List<int> SelectedComputerIds { get; set; } = new List<int>();

        [BindProperty(SupportsGet = true)]
        public string SearchQuery { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SortColumn { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool SortDescending { get; set; }

        public async Task OnGetAsync()
        {
            var currentTime = DateTime.UtcNow;

            var query = _context.Computers
                .Where(c => !c.DeletedAt.HasValue)
                .Include(c => c.Session) // Нужно для проверки активности
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                query = query.Where(c => c.ComputerIP.Contains(SearchQuery));
            }

            // Получаем список компьютеров с IP и статусом
            var computersWithStatus = await query
                .Select(c => new Computer
                {
                    Id = c.Id,
                    ComputerIP = c.ComputerIP,
                    Status = c.Session.Any(s => s.IsActive && (!s.SessionEndTime.HasValue || s.SessionEndTime > currentTime)),
                })
                .ToListAsync();

            // Сортировка
            Computers = SortColumn switch
            {
                "ComputerIP" => SortDescending
                    ? computersWithStatus.OrderByDescending(c => c.ComputerIP).ToList()
                    : computersWithStatus.OrderBy(c => c.ComputerIP).ToList(),
                "Status" => SortDescending
                    ? computersWithStatus.OrderByDescending(c => c.Status).ToList()
                    : computersWithStatus.OrderBy(c => c.Status).ToList(),
                _ => computersWithStatus.OrderBy(c => c.ComputerIP).ToList()
            };
        }

        public async Task<IActionResult> OnPostDeleteComputersAsync()
        {
            if (!string.IsNullOrWhiteSpace(Request.Form["SelectedComputerIds"]))
            {
                var selectedIds = Request.Form["SelectedComputerIds"]
                    .ToString()
                    .Split(',')
                    .Where(id => int.TryParse(id, out _))
                    .Select(int.Parse)
                    .ToList();

                if (selectedIds.Any())
                {
                    var computersToDelete = await _context.Computers
                        .Where(c => selectedIds.Contains(c.Id))
                        .ToListAsync();

                    if (computersToDelete.Any())
                    {
                        foreach (var computer in computersToDelete)
                        {
                            computer.DeletedAt = DateTime.UtcNow;
                        }

                        await _context.SaveChangesAsync();
                    }
                }
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetAllComputers()
        {
            var currentTime = DateTime.UtcNow;

            var computers = await _context.Computers
                .Where(c => !c.DeletedAt.HasValue)
                .Select(c => new
                {
                    id = c.Id,
                    computerIP = c.ComputerIP,
                    status = c.Session.Any(s => s.IsActive && (!s.SessionEndTime.HasValue || s.SessionEndTime > currentTime))
                })
                .ToListAsync();

            return new JsonResult(computers);
        }
    }
}
