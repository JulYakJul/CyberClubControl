using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace CybontrolX.Hubs
{
    public class UnlockHub : Hub
    {
        private readonly ILogger<UnlockHub> _logger;
        private static readonly ConcurrentDictionary<string, string> _connectionMap = new();

        public UnlockHub(ILogger<UnlockHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"[HUB] Новое подключение: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _connectionMap.TryRemove(Context.ConnectionId, out _);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task RegisterComputer(string computerIP)
        {
            _connectionMap[computerIP] = Context.ConnectionId;

            Console.WriteLine($"[HUB] Регистрация: IP={computerIP}, ConnectionId={Context.ConnectionId}");
            await Groups.AddToGroupAsync(Context.ConnectionId, computerIP);
        }

        // Метод для получения ConnectionId по IP
        public static string GetConnectionIdByIp(string ip)
        {
            return _connectionMap.TryGetValue(ip, out var connectionId) ? connectionId : null;
        }
    }
}