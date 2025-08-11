using Microsoft.Extensions.AI;

namespace CybontrolX.Interfaces
{
    public interface IChatService
    {
        Task<string> GetAIResponseAsync(List<ChatMessage> chatHistory);
    }
}
