using CybontrolX.DataBase;
using CybontrolX.DBModels;
using CybontrolX.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CybontrolX.Pages
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<Employee> _passwordHasher;
        private readonly IEmailService _emailService;

        public LoginModel(AppDbContext context, IPasswordHasher<Employee> passwordHasher, IEmailService emailService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
        }

        [BindProperty]
        public string UserName { get; set; }

        [BindProperty]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ReturnUrl { get; set; } = "/";

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserName == UserName);

            if (employee == null ||
                _passwordHasher.VerifyHashedPassword(employee, employee.PasswordHash, Password)
                != PasswordVerificationResult.Success)
            {
                ModelState.AddModelError("", "Неверный логин или пароль");
                return Page();
            }

            if (!employee.EmailConfirmed)
            {
                var newCode = new Random().Next(100000, 999999).ToString();
                var expiration = DateTime.UtcNow.AddMinutes(15);

                employee.ConfirmationCode = newCode;
                employee.CodeExpiration = expiration;
                await _context.SaveChangesAsync();

                await _emailService.SendConfirmationEmail(employee.UserName, newCode);

                return RedirectToPage("/ConfirmEmail", new { email = employee.UserName });
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, employee.UserName),
                new Claim(ClaimTypes.Role, employee.Role.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(new ClaimsPrincipal(identity));

            return Redirect(ReturnUrl);
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Login");
        }
    }
}