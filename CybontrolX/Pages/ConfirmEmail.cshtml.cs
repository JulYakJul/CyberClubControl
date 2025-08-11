using CybontrolX.DataBase;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using CybontrolX.Interfaces;

namespace CybontrolX.Pages
{
    [AllowAnonymous]
    public class ConfirmEmailModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public ConfirmEmailModel(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Введите код")]
        public string Code { get; set; }

        public IActionResult OnGet(string email)
        {
            Email = email;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _context.Employees
        .FirstOrDefaultAsync(u => u.UserName == Email);

            if (user == null || user.CodeExpiration < DateTime.UtcNow)
            {
                ModelState.AddModelError("", "Неверный или просроченный код");
                return Page();
            }

            if (user.ConfirmationCode != Code)
            {
                ModelState.AddModelError(nameof(Code), "Неверный код");
                return Page();
            }

            user.EmailConfirmed = true;
            user.ConfirmationCode = null;
            await _context.SaveChangesAsync();

            return RedirectToPage("/Login");
        }

        public async Task<IActionResult> OnPostResendCodeAsync()
        {
            Console.WriteLine("OnPostResendCodeAsync вызван");

            var user = await _context.Employees
                .FirstOrDefaultAsync(u => u.UserName == Email);

            if (user == null)
            {
                ModelState.AddModelError("", "Пользователь не найден");
                return Page();
            }

            var newCode = new Random().Next(100000, 999999).ToString();
            var expiration = DateTime.UtcNow.AddMinutes(15);

            user.ConfirmationCode = newCode;
            user.CodeExpiration = expiration;
            await _context.SaveChangesAsync();

            await _emailService.SendConfirmationEmail(user.UserName, newCode);

            TempData["Message"] = "Новый код подтверждения отправлен на ваш email.";
            return Page();
        }
    }
}