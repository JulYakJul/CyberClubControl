using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;
using CybontrolX.Services;
using Microsoft.Extensions.AI;
using CybontrolX.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CybontrolX.Pages
{
    public class ChatModel : PageModel
    {
        private readonly IChatService _chatService;

        public ChatModel(IChatService chatService)
        {
            _chatService = chatService;
        }

        private List<ChatMessage> FullChatHistory { get; set; } = new();

        public List<ChatMessage> DisplayChatHistory { get; set; } = new();

        [BindProperty]
        public string UserPrompt { get; set; }

        public async Task OnPostAsync()
        {
            if (FullChatHistory.Count == 0)
            {
                string systemPrompt;
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "AISupportPrompt.txt");

                if (System.IO.File.Exists(filePath))
                {
                    systemPrompt = await System.IO.File.ReadAllTextAsync(filePath);
                    FullChatHistory.Add(new ChatMessage(Microsoft.Extensions.AI.ChatRole.System, systemPrompt));
                }
            }

            FullChatHistory.Add(new ChatMessage(Microsoft.Extensions.AI.ChatRole.User, UserPrompt));
            DisplayChatHistory.Add(new ChatMessage(Microsoft.Extensions.AI.ChatRole.User, UserPrompt));

            var aiResponse = await _chatService.GetAIResponseAsync(FullChatHistory);
            var formattedResponse = FormatResponse(aiResponse);

            FullChatHistory.Add(new ChatMessage(Microsoft.Extensions.AI.ChatRole.Assistant, aiResponse));
            DisplayChatHistory.Add(new ChatMessage(Microsoft.Extensions.AI.ChatRole.Assistant, formattedResponse));
        }

        private string FormatResponse(string response)
        {
            response = response.Replace("*", "<span>").Replace("*", "</span>");
            response = response.Replace("\n", "<br />");
            return response;
        }
    }
}