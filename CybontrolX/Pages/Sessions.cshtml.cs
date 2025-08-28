using CybontrolX.DataBase;
using CybontrolX.DBModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CybontrolX.Pages
{
    [Authorize(Roles = "Admin, Manager")]
    public class SessionsModel : PageModel
    {
        private readonly AppDbContext _context;

        public SessionsModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<Session> Sessions { get; set; } = new List<Session>();

        [BindProperty(SupportsGet = true)]
        public string SearchQuery { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SortColumn { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool SortDescending { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.Sessions
                .Include(s => s.Client)
                .Include(s => s.Computer)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                query = query.Where(s => s.Client != null && s.Client.Name.Contains(SearchQuery));
            }

            query = SortColumn switch
            {
                "Name" => SortDescending
                    ? query.OrderByDescending(s => s.Client != null ? s.Client.Name : null)
                    : query.OrderBy(s => s.Client != null ? s.Client.Name : null),
                "ComputerId" => SortDescending
                    ? query.OrderByDescending(s => s.Computer != null ? s.Computer.ComputerIP : null)
                    : query.OrderBy(s => s.Computer != null ? s.Computer.ComputerIP : null),
                "SessionStartTime" => SortDescending
                    ? query.OrderByDescending(s => s.SessionStartTime)
                    : query.OrderBy(s => s.SessionStartTime),
                "SessionEndTime" => SortDescending
                    ? query.OrderByDescending(s => s.SessionEndTime)
                    : query.OrderBy(s => s.SessionEndTime),
                _ => query.OrderBy(s => s.SessionStartTime),
            };

            Sessions = await query.ToListAsync();
        }

        public async Task<IActionResult> OnGetAllSessions()
        {
            var sessions = await _context.Sessions
                .Select(s => new
                {
                    id = s.Id,
                    clientName = s.Client != null ? s.Client.Name : null,
                    computerIP = s.Computer != null ? s.Computer.ComputerIP : null,
                    sessionStartTime = s.SessionStartTime.ToString("o"),
                    sessionEndTime = s.SessionEndTime != null ? s.SessionEndTime.Value.ToString("o") : null,
                    isActive = s.IsActive
                })
                .ToListAsync();

            return new JsonResult(sessions);
        }

        public async Task<IActionResult> OnPostDeleteSessions([FromForm] string SelectedSessionIds)
        {
            if (!string.IsNullOrEmpty(SelectedSessionIds))
            {
                var ids = SelectedSessionIds.Split(',').Select(int.Parse).ToList();
                var sessionsToClose = await _context.Sessions
                    .Where(s => ids.Contains(s.Id) && s.IsActive)
                    .ToListAsync();

                if (sessionsToClose.Any())
                {
                    foreach (var session in sessionsToClose)
                    {
                        session.IsActive = false;
                        session.SessionEndTime = DateTime.UtcNow;
                    }
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToPage();
        }
    }
}