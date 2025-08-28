using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Sockets;
using CybontrolX.DBModels;
using CybontrolX.DataBase;
using Microsoft.AspNetCore.Authorization;
using CybontrolX.Interfaces;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace CybontrolX.Pages
{
    [Authorize(Roles = "Admin, Manager")]
    public class CreateComputerModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly INetworkService _networkService;

        public CreateComputerModel(AppDbContext context, INetworkService networkService)
        {
            _context = context;
            _networkService = networkService;
        }

        [BindProperty]
        public Computer NewComputer { get; set; }

        public string NotificationMessage { get; set; }

        [BindProperty]
        public int Port { get; set; }

        public IActionResult OnPostAddComputer()
        {
            if (_networkService.IsPortOpen(NewComputer.ComputerIP, Port, 2000))
            {
                if (_context.Computers.Any(c => c.ComputerIP == NewComputer.ComputerIP && c.DeletedAt == null))
                {
                    NotificationMessage = $"Компьютер с IP {NewComputer.ComputerIP} уже существует!";
                    return Page();
                }

                var computer = new Computer
                {
                    ComputerIP = NewComputer.ComputerIP
                };

                _context.Computers.Add(computer);
                _context.SaveChanges();

                NotificationMessage = $"Компьютер {NewComputer.ComputerIP} с открытым портом {Port} успешно добавлен!";
            }
            else
            {
                NotificationMessage = $"Порт {Port} на компьютере {NewComputer.ComputerIP} закрыт или недоступен";
            }

            return Page();
        }
    }
}
