using CybontrolX.DataBase;
using Microsoft.EntityFrameworkCore;

namespace CybontrolX.Services
{
    public class SessionCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SessionCleanupService> _logger;

        public SessionCleanupService(IServiceScopeFactory scopeFactory, ILogger<SessionCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SessionCleanupService запущен.");

            await UpdateExpiredSessions(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                    await UpdateExpiredSessions(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при выполнении шедулера.");
                }
            }
        }

        private async Task UpdateExpiredSessions(CancellationToken stoppingToken)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var currentTime = DateTime.UtcNow;
                _logger.LogInformation($"Текущее время (UTC): {currentTime:yyyy-MM-dd HH:mm:ss}");

                var sessions = await dbContext.Sessions.ToListAsync(stoppingToken);
                foreach (var session in sessions)
                {
                    DateTime? sessionEndTimeUtc = session.SessionEndTime?.ToUniversalTime();
                    _logger.LogInformation($"ID Сессии: {session.Id}, Время окончания (UTC): {sessionEndTimeUtc:yyyy-MM-dd HH:mm:ss}, Активна: {session.IsActive}");
                }

                var expiredSessions = await dbContext.Sessions
                    .Where(s => s.IsActive && s.SessionEndTime.HasValue && s.SessionEndTime.Value <= currentTime)
                    .ToListAsync(stoppingToken);

                if (expiredSessions.Any())
                {
                    foreach (var session in expiredSessions)
                    {
                        session.IsActive = false;
                    }

                    await dbContext.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation($"Обновлено {expiredSessions.Count} сессий.");
                }
                else
                {
                    _logger.LogInformation("Не найдено истекших сессий для обновления.");
                }
            }
        }
    }
}
