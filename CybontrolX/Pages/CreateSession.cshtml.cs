using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using CybontrolX.DBModels;
using System.Collections.Generic;
using System.Linq;
using CybontrolX.DataBase;
using Yandex.Checkout.V3;
using System.Globalization;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR.Client;

namespace CybontrolX.Pages
{
    [Authorize(Roles = "Admin, Manager")]
    public class CreateSessionModel : PageModel
    {
        private readonly AppDbContext _context;
        public static readonly Yandex.Checkout.V3.Client _client = new Yandex.Checkout.V3.Client("1042786", "test_8c5gXfEfua9p-oLMCzWKE_BGXg7Ps4ky1CZ7NSO9U1g");

        public CreateSessionModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Session Session { get; set; } = new Session();

        [BindProperty]
        public int[] SelectedTariffs { get; set; } = new int[0];

        [BindProperty]
        public Dictionary<int, int> TariffQuantities { get; set; } = new Dictionary<int, int>();

        [BindProperty]
        public string NewClientFullName { get; set; }

        [BindProperty]
        public string NewClientPhoneNumber { get; set; }

        [BindProperty]
        public string ExistingClientPhoneNumber { get; set; }

        [BindProperty]
        public string PaymentMethod { get; set; } = "YooKassa";

        public List<SelectListItem> ClientList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> ComputerList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> EmployeeList { get; set; } = new List<SelectListItem>();
        public List<Tariff> Tariffs { get; set; } = new List<Tariff>();

        private static readonly TimeSpan UtcPlus4Offset = TimeSpan.FromHours(4);

        public void OnGet()
        {
            var today = DateTime.UtcNow.DayOfWeek;

            var clients = _context.Clients
                .Where(c => c.DeletedAt == null)
                .ToList();

            var employees = _context.Employees
                .Where(e => e.DeletedAt == null)
                .ToList();

            Tariffs = _context.Tariffs
                .Where(t => t.Days.Contains(today))
                .ToList() ?? new List<Tariff>();

            var activeComputerIds = _context.Sessions
                .Where(s => s.IsActive)
                .Select(s => s.ComputerId)
                .ToHashSet();

            var availableComputers = _context.Computers
                .Where(c => !activeComputerIds.Contains(c.Id) && c.DeletedAt == null)
                .ToList();

            ClientList = clients.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            }).ToList();

            if (availableComputers.Any())
            {
                ComputerList = availableComputers.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.ComputerIP
                }).ToList();
            }
            else
            {
                ComputerList.Add(new SelectListItem
                {
                    Value = null,
                    Text = "Нет свободных компьютеров"
                });
            }

            EmployeeList = employees.Select(e => new SelectListItem
            {
                Value = e.Id.ToString(),
                Text = e.Surname
            }).ToList();
        }

        public async Task<IActionResult> OnPostCreateSession()
        {
            ModelState.Clear();
            CybontrolX.DBModels.Client client = null;

            if (!string.IsNullOrEmpty(ExistingClientPhoneNumber))
            {
                client = _context.Clients.FirstOrDefault(c => c.PhoneNumber == ExistingClientPhoneNumber);
            }
            if (client == null && !string.IsNullOrEmpty(NewClientFullName) && !string.IsNullOrEmpty(NewClientPhoneNumber))
            {
                client = new CybontrolX.DBModels.Client
                {
                    Name = NewClientFullName,
                    PhoneNumber = NewClientPhoneNumber
                };
                _context.Clients.Add(client);
                await _context.SaveChangesAsync();
            }

            if (client == null || Session.ComputerId == 0)
            {
                TempData["NotificationMessage"] = "Выберите клиента и компьютер.";
                return Page();
            }
            if (SelectedTariffs == null || SelectedTariffs.Length == 0)
            {
                TempData["NotificationMessage"] = "Выберите хотя бы один тариф.";
                return Page();
            }
            if (Session.EmployeeId == 0)
            {
                TempData["NotificationMessage"] = "Выберите сотрудника.";
                return Page();
            }

            var employeeExists = _context.Employees.Any(e => e.Id == Session.EmployeeId);
            if (!employeeExists)
            {
                TempData["NotificationMessage"] = "Сотрудник не найден.";
                return Page();
            }

            decimal totalAmount = CalculateTotalAmount();
            var sessionStartTimeOffset = DateTimeOffset.UtcNow.ToOffset(UtcPlus4Offset);
            var sessionStartTime = sessionStartTimeOffset.DateTime;
            var sessionEndTime = CalculateSessionEndTime(sessionStartTime);

            if (PaymentMethod == "Cash")
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var session = new Session
                    {
                        ClientId = client.Id,
                        ComputerId = Session.ComputerId,
                        EmployeeId = Session.EmployeeId,
                        SessionStartTime = sessionStartTime,
                        SessionEndTime = sessionEndTime.DateTime,
                        IsActive = true,
                        PaymentStatus = true,
                        PaymentYooKassaId = "CASH_" + Guid.NewGuid().ToString()
                    };

                    _context.Sessions.Add(session);
                    await _context.SaveChangesAsync();

                    foreach (var tariffId in SelectedTariffs)
                    {
                        if (TariffQuantities.TryGetValue(tariffId, out int quantity) && quantity > 0)
                        {
                            _context.SessionTariffs.Add(new SessionTariff
                            {
                                SessionId = session.Id,
                                TariffId = tariffId,
                                Quantity = quantity
                            });
                        }
                    }

                    // Создаем запись о платеже
                    var payment = new Payment
                    {
                        Amount = totalAmount,
                        Currency = "RUB",
                        Status = "Succeeded",
                        Description = $"Оплата наличными за сеанс",
                        PaymentDateTime = DateTime.UtcNow,
                        PaymentMethod = "Cash",
                        ClientId = client.Id,
                        EmployeeId = Session.EmployeeId,
                        SessionId = session.Id,
                        PaymentYooKassaId = session.PaymentYooKassaId
                    };

                    _context.Payments.Add(payment);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["NotificationMessage"] = "Сессия успешно создана (оплата наличными)";
                    return RedirectToPage("Sessions");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", $"Ошибка при создании сессии: {ex.Message}");
                    return Page();
                }
            }
            else
            {
                var sessionData = new
                {
                    ClientId = client.Id,
                    ComputerId = Session.ComputerId,
                    EmployeeId = Session.EmployeeId,
                    SelectedTariffs = SelectedTariffs,
                    TariffQuantities = TariffQuantities,
                    SessionStartTime = sessionStartTime,
                    SessionEndTime = sessionEndTime
                };

                var newPayment = new NewPayment
                {
                    Amount = new Amount { Value = totalAmount, Currency = "RUB" },
                    Confirmation = new Confirmation
                    {
                        Type = ConfirmationType.Redirect,
                        ReturnUrl = Url.Page("CreateSession", "PaymentSuccess", null, Request.Scheme)
                    },
                    Capture = false,
                    Metadata = new Dictionary<string, string>
                    {
                        { "session_data", JsonConvert.SerializeObject(sessionData) }
                    }
                };

                try
                {
                    var payment = _client.CreatePayment(newPayment);
                    TempData["PaymentId"] = payment.Id;

                    return Redirect(payment.Confirmation.ConfirmationUrl);
                }
                catch (Yandex.Checkout.V3.YandexCheckoutException ex)
                {
                    ModelState.AddModelError("", $"Ошибка при создании платежа: {ex.Message}");
                    return Page();
                }
            }
        }

        private DateTimeOffset CalculateSessionEndTime(DateTimeOffset startTime)
        {
            TimeSpan totalDuration = TimeSpan.Zero;

            foreach (var tariffId in SelectedTariffs)
            {
                if (TariffQuantities.TryGetValue(tariffId, out int quantity) && quantity > 0)
                {
                    var tariff = _context.Tariffs.FirstOrDefault(t => t.Id == tariffId);
                    if (tariff != null)
                    {
                        totalDuration += tariff.SessionTime * quantity;
                    }
                }
            }

            return startTime + totalDuration;
        }

        public IActionResult OnGetSearchClients(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
            {
                return new JsonResult(new List<string>());
            }

            var matchingClients = _context.Clients
                .Where(c => c.PhoneNumber.StartsWith(phoneNumber) && c.DeletedAt == null)
                .Select(c => c.PhoneNumber)
                .ToList();

            return new JsonResult(matchingClients);
        }

        private decimal CalculateTotalAmount()
        {
            decimal totalAmount = 0;
            foreach (var tariffId in SelectedTariffs)
            {
                if (TariffQuantities.TryGetValue(tariffId, out int quantity) && quantity > 0)
                {
                    var tariff = _context.Tariffs.FirstOrDefault(t => t.Id == tariffId);
                    if (tariff != null)
                    {
                        totalAmount += (decimal)(tariff.Price * quantity);
                    }
                }
            }
            return totalAmount;
        }
    }
}