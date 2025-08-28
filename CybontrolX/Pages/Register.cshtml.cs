using CybontrolX.DataBase;
using CybontrolX.DBModels;
using CybontrolX.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;
using MimeKit;
using CybontrolX.Models;

namespace CybontrolX.Pages.Auth
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<Employee> _passwordHasher;
        private readonly IEmailService _emailService;

        public RegisterModel(AppDbContext context,
                            IPasswordHasher<Employee> passwordHasher,
                            IEmailService emailService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
        }

        [BindProperty]
        public RegisterInputModel Input { get; set; }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var code = new Random().Next(100000, 999999).ToString();
            var expiration = DateTime.UtcNow.AddMinutes(15);

            if (await _context.Employees.AnyAsync(e => e.UserName == Input.UserName))
            {
                ModelState.AddModelError("Input.UserName", "Этот логин уже занят");
                return Page();
            }

            var employee = new Employee
            {
                Name = Input.Name,
                Surname = Input.Surname,
                Patronymic = Input.Patronymic,
                PhoneNumber = Input.PhoneNumber,
                UserName = Input.UserName,
                Role = Input.Role,
                Status = "Active",
                ConfirmationCode = code,
                CodeExpiration = expiration
            };

            employee.PasswordHash = _passwordHasher.HashPassword(employee, Input.Password);

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            try
            {
                await _emailService.SendConfirmationEmail(Input.UserName, code);
                return RedirectToPage("/ConfirmEmail", new { email = Input.UserName });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Не удалось отправить письмо. Попробуйте позже.");
                return Page();
            }
        }
    }
}