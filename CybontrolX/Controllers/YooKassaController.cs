using Microsoft.AspNetCore.Mvc;
using Yandex.Checkout.V3;
using System.IO;
using System;
using CybontrolX.DataBase;
using CybontrolX.DBModels;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using CybontrolX.Models;
using Microsoft.AspNetCore.Authorization;
using CybontrolX.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CybontrolX.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class YooKassaController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<YooKassaController> _logger;
        private readonly IHubContext<UnlockHub> _hubContext;

        public YooKassaController(
            AppDbContext context,
            ILogger<YooKassaController> logger,
            IHubContext<UnlockHub> hubContext)
        {
            _context = context;
            _logger = logger;
            _hubContext = hubContext;
        }

        static readonly Yandex.Checkout.V3.Client _client = new Yandex.Checkout.V3.Client(
            "1042786", // Shop ID
            "test_8c5gXfEfua9p-oLMCzWKE_BGXg7Ps4ky1CZ7NSO9U1g" // Secret Key
        );

        [HttpPost("notify")]
        public async Task<IActionResult> Notify()
        {
            try
            {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                _logger.LogInformation("[YooKassa Notify] Received request: {Body}", body);

                var notification = Yandex.Checkout.V3.Client.ParseMessage(
                    Request.Method,
                    Request.ContentType,
                    new MemoryStream(Encoding.UTF8.GetBytes(body))
                );

                _logger.LogInformation("[YooKassa Notify] Notification type: {NotificationType}", notification.GetType().Name);

                switch (notification)
                {
                    case PaymentWaitingForCaptureNotification captureNotification:
                        var payment = captureNotification.Object;
                        if (payment.Paid)
                        {
                            _client.CapturePayment(payment.Id);
                            await ProcessPaymentAsync(payment);
                        }
                        break;

                    case PaymentSucceededNotification successNotification:
                        await ProcessPaymentAsync(successNotification.Object);
                        break;
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обработки уведомления");
                return BadRequest("Ошибка обработки уведомления");
            }
        }

        private async Task ProcessPaymentAsync(Yandex.Checkout.V3.Payment payment)
        {
            if (payment.Metadata.TryGetValue("payment_data", out string paymentDataJson))
            {
                await ProcessProductPaymentAsync(payment, paymentDataJson);
            }
            else if (payment.Metadata.TryGetValue("session_data", out string sessionDataJson))
            {
                await UpdateSessionPaymentStatusAsync(payment, sessionDataJson);
            }
        }

        private async Task ProcessProductPaymentAsync(Yandex.Checkout.V3.Payment payment, string paymentDataJson)
        {
            try
            {
                _logger.LogInformation("Начало обработки платежа. Payment ID: {PaymentId}", payment.Id);

                if (payment.Status != PaymentStatus.Succeeded || !payment.Paid)
                {
                    _logger.LogInformation("Платеж не успешен или не оплачен. Status: {Status}, Paid: {Paid}",
                        payment.Status, payment.Paid);
                    return;
                }

                // Десериализация данных
                var paymentData = JsonConvert.DeserializeObject<dynamic>(paymentDataJson);
                if (paymentData == null)
                {
                    _logger.LogError("Не удалось десериализовать payment_data");
                    return;
                }

                // Проверяем существование платежа
                var existingPayment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.PaymentYooKassaId == payment.Id);

                if (existingPayment != null)
                {
                    _logger.LogInformation("Платеж уже существует в базе");
                    return;
                }

                // Создаем запись о платеже
                var paymentEntity = new Payment
                {
                    PaymentYooKassaId = payment.Id,
                    Amount = payment.Amount?.Value ?? 0,
                    Currency = payment.Amount?.Currency ?? "RUB",
                    Status = payment.Status.ToString(),
                    Description = payment.Description ?? "Оплата товаров",
                    PaymentDateTime = DateTime.UtcNow,
                    PaymentMethod = payment.PaymentMethod?.Type.ToString() ?? "Unknown",
                    ClientId = paymentData.ClientId,
                    EmployeeId = paymentData.EmployeeId
                };

                _context.Payments.Add(paymentEntity);

                // Обновляем количество товаров
                foreach (int productId in paymentData.SelectedProducts)
                {
                    string productIdStr = productId.ToString();
                    if (paymentData.ProductQuantities[productIdStr] == null)
                        continue;

                    int quantity = (int)paymentData.ProductQuantities[productIdStr];
                    var product = await _context.Products.FindAsync(productId);

                    if (product == null)
                    {
                        _logger.LogError($"Товар с ID {productId} не найден");
                        continue;
                    }

                    if (product.Quantity < quantity)
                    {
                        _logger.LogError($"Недостаточно товара {product.Name} в наличии. Требуется: {quantity}, Доступно: {product.Quantity}");
                        continue;
                    }

                    product.Quantity -= quantity;
                    _context.Products.Update(product);
                    _logger.LogInformation($"Обновлено количество товара {product.Name}. Новое количество: {product.Quantity}");
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Платеж успешно обработан и сохранен в базу данных");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обработки платежа");
            }
        }

        private async Task UpdateSessionPaymentStatusAsync(Yandex.Checkout.V3.Payment payment, string sessionDataJson)
        {
            try
            {
                var sessionData = JsonConvert.DeserializeObject<SessionDataModel>(sessionDataJson);

                // Проверка существующего платежа
                var existingPayment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.PaymentYooKassaId == payment.Id);

                if (existingPayment != null)
                {
                    _logger.LogInformation($"Платеж {payment.Id} уже существует");
                    return;
                }

                // Создаем или получаем сессию
                var sessionId = await GetOrCreateSessionId(sessionData, payment.Id);
                var session = await _context.Sessions.FindAsync(sessionId);
                var computer = await _context.Computers.FirstOrDefaultAsync(c => c.Id == sessionData.ComputerId);

                if (session == null)
                {
                    _logger.LogError($"Сессия не найдена для ID: {sessionId}");
                    return;
                }

                // Создаем запись о платеже
                var paymentEntity = new Payment
                {
                    PaymentYooKassaId = payment.Id,
                    Amount = payment.Amount?.Value ?? 0,
                    Currency = payment.Amount?.Currency ?? "RUB",
                    Status = payment.Status.ToString(),
                    Description = payment.Description ?? "Оплата через YooKassa",
                    PaymentDateTime = DateTime.UtcNow,
                    PaymentMethod = payment.PaymentMethod?.Type.ToString() ?? "Unknown",
                    ClientId = sessionData.ClientId,
                    EmployeeId = sessionData.EmployeeId,
                    SessionId = sessionId
                };

                _context.Payments.Add(paymentEntity);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Создан платеж ID: {paymentEntity.Id}, Сумма: {paymentEntity.Amount}");

                var computerIP = computer.ComputerIP;
                var connectionId = UnlockHub.GetConnectionIdByIp(computerIP);

                if (string.IsNullOrEmpty(connectionId))
                {
                    _logger.LogError($"Нет активного подключения для IP {computerIP}");
                    return;
                }

                // Отправка команды разблокировки
                _logger.LogInformation($"Попытка разблокировать компьютер {sessionData.ComputerId}");

                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                    var unlockMessage = new
                    {
                        Command = "UnlockComputer",
                        UnlockUntil = session.SessionEndTime
                    };

                    await _hubContext.Clients.Client(connectionId).SendAsync("UnlockComputer",
                        JsonConvert.SerializeObject(unlockMessage));

                    _logger.LogInformation($"Команда разблокировки отправлена на компьютер {sessionData.ComputerId}, " +
                                          $"разблокирован до {session.SessionEndTime}");
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning($"Таймаут при отправке команды разблокировки на компьютер {sessionData.ComputerId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Ошибка при отправке команды разблокировки на компьютер {sessionData.ComputerId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обработки платежа и разблокировки компьютера");
                throw;
            }
        }

        private async Task<int?> GetOrCreateSessionId(SessionDataModel sessionData, string paymentId)
        {
            var existingSession = await _context.Sessions
                .FirstOrDefaultAsync(s => s.PaymentYooKassaId == paymentId);

            if (existingSession != null)
            {
                return existingSession.Id;
            }

            var session = new Session
            {
                ClientId = sessionData.ClientId,
                ComputerId = sessionData.ComputerId,
                EmployeeId = sessionData.EmployeeId,
                PaymentStatus = true,
                PaymentYooKassaId = paymentId,
                SessionStartTime = sessionData.SessionStartTime,
                IsActive = true,
                SessionEndTime = CalculateSessionEndTime(sessionData)
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            foreach (var tariffId in sessionData.SelectedTariffs)
            {
                if (sessionData.TariffQuantities.TryGetValue(tariffId, out int quantity))
                {
                    _context.SessionTariffs.Add(new SessionTariff
                    {
                        SessionId = session.Id,
                        TariffId = tariffId,
                        Quantity = quantity
                    });
                }
            }

            await _context.SaveChangesAsync();
            return session.Id;
        }

        private DateTime CalculateSessionEndTime(SessionDataModel sessionData)
        {
            TimeSpan total = TimeSpan.Zero;
            foreach (var tariffId in sessionData.SelectedTariffs)
            {
                if (sessionData.TariffQuantities.TryGetValue(tariffId, out int quantity))
                {
                    var tariff = _context.Tariffs.FirstOrDefault(t => t.Id == tariffId);
                    if (tariff != null)
                    {
                        total += tariff.SessionTime * quantity;
                    }
                }
            }
            return sessionData.SessionStartTime + total;
        }
    }
}