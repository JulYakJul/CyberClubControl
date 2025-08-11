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
using CybontrolX.Controllers;
using Microsoft.EntityFrameworkCore;

namespace CybontrolX.Pages
{
    [Authorize(Roles = "Admin, Manager")]
    public class ProductPaymentModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ProductPaymentModel> _logger;
        public static readonly Yandex.Checkout.V3.Client _client = new Yandex.Checkout.V3.Client("1042786", "test_8c5gXfEfua9p-oLMCzWKE_BGXg7Ps4ky1CZ7NSO9U1g");

        public ProductPaymentModel(AppDbContext context, ILogger<ProductPaymentModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public int[] SelectedProducts { get; set; } = new int[0];

        [BindProperty]
        public Dictionary<int, int> ProductQuantities { get; set; } = new Dictionary<int, int>();

        [BindProperty]
        public string NewClientFullName { get; set; }

        [BindProperty]
        public string NewClientPhoneNumber { get; set; }

        [BindProperty]
        public string ExistingClientPhoneNumber { get; set; }

        [BindProperty]
        public string PaymentMethod { get; set; } = "YooKassa";

        [BindProperty]
        public int EmployeeId { get; set; }

        public List<SelectListItem> ClientList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> EmployeeList { get; set; } = new List<SelectListItem>();
        public List<Product> Products { get; set; } = new List<Product>();

        public void OnGet()
        {
            var clients = _context.Clients.ToList();
            var employees = _context.Employees
                .Where(e => e.DeletedAt == null)
                .ToList();

            Products = _context.Products
                .Where(p => p.Quantity > 0)
                .ToList() ?? new List<Product>();

            ClientList = clients.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            }).ToList();

            EmployeeList = employees.Select(e => new SelectListItem
            {
                Value = e.Id.ToString(),
                Text = e.Surname
            }).ToList();
        }

        public async Task<IActionResult> OnPostProcessPayment()
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

            if (SelectedProducts == null || SelectedProducts.Length == 0)
            {
                TempData["NotificationMessage"] = "Выберите хотя бы один товар.";
                return Page();
            }
            if (EmployeeId == 0)
            {
                TempData["NotificationMessage"] = "Выберите сотрудника.";
                return Page();
            }

            var employeeExists = _context.Employees.Any(e => e.Id == EmployeeId);
            if (!employeeExists)
            {
                TempData["NotificationMessage"] = "Сотрудник не найден.";
                return Page();
            }

            decimal totalAmount = 0;
            var productsToUpdate = new List<Product>();

            foreach (var productId in SelectedProducts)
            {
                if (ProductQuantities.TryGetValue(productId, out int quantity) && quantity > 0)
                {
                    var product = _context.Products.FirstOrDefault(p => p.Id == productId);
                    if (product == null)
                    {
                        TempData["NotificationMessage"] = $"Товар с ID {productId} не найден.";
                        return Page();
                    }

                    if (product.Quantity < quantity)
                    {
                        TempData["NotificationMessage"] = $"Недостаточно товара {product.Name} в наличии (доступно: {product.Quantity}).";
                        return Page();
                    }

                    totalAmount += (decimal)(product.SalePrice * quantity);
                    productsToUpdate.Add(product);
                }
            }

            if (PaymentMethod == "Cash")
            {
                try
                {
                    foreach (var product in productsToUpdate)
                    {
                        if (ProductQuantities.TryGetValue(product.Id, out int quantity))
                        {
                            product.Quantity -= quantity;
                        }
                    }

                    var payment = new Payment
                    {
                        Amount = totalAmount,
                        Currency = "RUB",
                        Status = "Success",
                        Description = $"Оплата наличными за товары",
                        PaymentMethod = "Cash",
                        PaymentDateTime = DateTime.UtcNow,
                        ClientId = client?.Id,
                        EmployeeId = EmployeeId,
                        PaymentYooKassaId = "CASH_PAYMENT"
                    };


                    _context.Payments.Add(payment);
                    await _context.SaveChangesAsync();

                    TempData["NotificationMessage"] = "Оплата успешно проведена (наличные)";
                    return RedirectToPage("Products");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Ошибка при обработке платежа: {ex.Message}");
                    return Page();
                }
            }
            else
            {
                var paymentData = new
                {
                    ClientId = client?.Id,
                    EmployeeId = EmployeeId,
                    SelectedProducts = SelectedProducts,
                    ProductQuantities = ProductQuantities
                };

                var newPayment = new NewPayment
                {
                    Amount = new Amount { Value = totalAmount, Currency = "RUB" },
                    Confirmation = new Confirmation
                    {
                        Type = ConfirmationType.Redirect,
                        ReturnUrl = Url.Page("ProductPayment", "PaymentSuccess", null, Request.Scheme)
                    },
                    Capture = false,
                    Metadata = new Dictionary<string, string>
                    {
                        { "payment_data", JsonConvert.SerializeObject(paymentData) }
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
                    TempData["NotificationMessage"] = $"Ошибка при создании платежа: {ex.Message}";
                    return Page();
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Ошибка: {ex.Message}");
                    TempData["NotificationMessage"] = $"Ошибка: {ex.Message}";
                    return Page();
                }
            }
        }

        public async Task<IActionResult> OnGetPaymentSuccess()
        {
            var paymentId = TempData["PaymentId"]?.ToString();
            if (string.IsNullOrEmpty(paymentId))
            {
                TempData["NotificationMessage"] = "Не найден идентификатор платежа";
                return RedirectToPage("Products");
            }

            try
            {
                var payment = _client.GetPayment(paymentId);

                var existingPayment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.PaymentYooKassaId == paymentId);

                if (existingPayment != null)
                {
                    TempData["NotificationMessage"] = "Платеж уже обработан";
                    return RedirectToPage("Products");
                }

                TempData["NotificationMessage"] = "Платеж в процессе обработки. Статус: " + payment.Status;
                return RedirectToPage("Products");
            }
            catch (Exception ex)
            {
                TempData["NotificationMessage"] = $"Ошибка при проверке платежа: {ex.Message}";
                return RedirectToPage("Products");
            }
        }

        public IActionResult OnGetSearchClients(string phoneNumber)
        {
            _logger.LogInformation("Поиск клиента по номеру: {PhoneNumber}", phoneNumber);

            if (string.IsNullOrEmpty(phoneNumber))
            {
                return new JsonResult(new List<string>());
            }

            var matchingClients = _context.Clients
                .Where(c => c.PhoneNumber.StartsWith(phoneNumber))
                .Select(c => c.PhoneNumber)
                .ToList();

            return new JsonResult(matchingClients);
        }

        public IActionResult OnGetSearchProducts(string query, string selectedIds)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new JsonResult(new List<object>());
            }

            var matchingProducts = _context.Products
                .Where(p => p.Name.ToLower().Contains(query.ToLower()) && p.Quantity > 0)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.SalePrice,
                    p.Quantity
                })
                .ToList();

            return new JsonResult(matchingProducts);
        }

    }
}