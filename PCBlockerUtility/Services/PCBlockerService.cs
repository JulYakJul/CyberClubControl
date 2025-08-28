using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Console;
using Microsoft.AspNetCore.Http.Connections;

namespace PCBlockerUtility.Services
{
    public class PCBlockerService : IDisposable
    {
        private readonly string _hubUrl;
        private readonly string _computerId;
        private readonly Action<DateTime> _onUnlockReceived;
        private readonly Action _onLockReceived;
        private HubConnection _connection;
        private readonly ILogger _logger;

        public PCBlockerService(
            string hubUrl,
            string computerId,
            Action<DateTime> onUnlockReceived,
            Action onLockReceived,
            ILogger logger = null)
        {
            _hubUrl = hubUrl;
            _computerId = computerId;
            _onUnlockReceived = onUnlockReceived;
            _onLockReceived = onLockReceived;
            _logger = logger ?? CreateDefaultLogger();
        }

        private ILogger CreateDefaultLogger()
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            return loggerFactory.CreateLogger<PCBlockerService>();
        }

        public async Task StartAsync()
        {
            try
            {
                _logger.LogInformation($"Попытка подключения к {_hubUrl}...");

                _connection = new HubConnectionBuilder()
                    .WithUrl(_hubUrl, options =>
                    {
                        options.HttpMessageHandlerFactory = _ => new HttpClientHandler
                        {
                            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                        };
                    })
                    .ConfigureLogging(logging =>
                    {
                        logging.AddConsole();
                        logging.SetMinimumLevel(LogLevel.Debug);
                    })
                    .Build();

                SetupConnectionHandlers();

                await _connection.StartAsync();
                _logger.LogInformation("Подключение к SignalR Hub успешно установлено!");

                await _connection.InvokeAsync("RegisterComputer", GetLocalIPAddress());
                _logger.LogInformation($"Компьютер {_computerId} зарегистрирован на сервере.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка подключения к Hub: {ex.GetType().Name}");
                throw;
            }
        }

        private void SetupConnectionHandlers()
        {
            _connection.On<string>("UnlockComputer", (messageJson) =>
            {
                try
                {
                    var message = JsonConvert.DeserializeObject<UnlockMessage>(messageJson);
                    _logger.LogInformation($"Получена команда разблокировки до {message.UnlockUntil}");

                    _onUnlockReceived?.Invoke(message.UnlockUntil);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка обработки сообщения разблокировки");
                }
            });

            _connection.On("LockComputer", OnLockComputer);

            _connection.Closed += async (error) =>
            {
                _logger.LogWarning($"Соединение закрыто. Причина: {error?.Message ?? "неизвестна"}");
                await TryReconnectAsync();
            };

            _connection.Reconnected += async (connectionId) =>
            {
                _logger.LogInformation($"Переподключено с ID: {connectionId}");
                await _connection.InvokeAsync("RegisterComputer", GetLocalIPAddress());
            };

            _connection.Reconnecting += (error) =>
            {
                _logger.LogWarning($"Попытка переподключения... Ошибка: {error?.Message}");
                return Task.CompletedTask;
            };
        }

        private async Task TryReconnectAsync()
        {
            int maxAttempts = 5;
            for (int i = 0; i < maxAttempts; i++)
            {
                try
                {
                    await _connection.StartAsync();
                    _logger.LogInformation("Переподключение успешно!");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Попытка {i + 1}/{maxAttempts} неудачна");
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            }
            _logger.LogError("Не удалось переподключиться после всех попыток!");
        }

        private async void OnUnlockComputer(DateTime until)
        {
            try
            {
                _logger?.LogInformation($"Received unlock command until {until}");
                
                _logger?.LogInformation($"Current connection state: {_connection.State}");

                _onUnlockReceived?.Invoke(until);
                await _connection.InvokeAsync("ConfirmUnlock", _computerId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error processing unlock command");
            }
        }

        private string GetLocalIPAddress()
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "unknown";
        }

        private void OnLockComputer()
        {
            _onLockReceived?.Invoke();
        }

        public async Task StopAsync()
        {
            if (_connection != null)
            {
                await _connection.StopAsync();
                await _connection.DisposeAsync();
                _connection = null;
            }
        }

        public void Dispose()
        {
            StopAsync().GetAwaiter().GetResult();
        }

        public async ValueTask DisposeAsync()
        {
            if (_connection != null)
            {
                await _connection.DisposeAsync();
            }
        }

        private class UnlockMessage
        {
            public string Command { get; set; }
            public DateTime UnlockUntil { get; set; }
        }
    }
}
