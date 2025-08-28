using Microsoft.Extensions.AI;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CybontrolX.Interfaces;

namespace CybontrolX.Services
{
    public class ChatService : IChatService
    {
        private readonly IChatClient _chatClient;

        public ChatService()
        {
            _chatClient = new OllamaChatClient(new Uri("http://localhost:11434/"), "gemma2:2b");
        }

        public async Task<string> GetAIResponseAsync(List<ChatMessage> chatHistory)
        {
            var response = "";
            await foreach (var item in _chatClient.GetStreamingResponseAsync(chatHistory))
            {
                System.Console.WriteLine($"Received chunk: {item.Text}");
                response += item.Text;
            }
            return response;
        }
    }
}