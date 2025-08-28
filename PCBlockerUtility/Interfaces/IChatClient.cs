using PCBlockerUtility.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PCBlockerUtility.Interfaces
{
    public interface IChatClient
    {
        Task<string> GetAIResponseAsync(List<ChatMessage> chatHistory);
    }
}