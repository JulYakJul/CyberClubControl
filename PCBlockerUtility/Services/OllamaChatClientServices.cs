using PCBlockerUtility.Interfaces;
using PCBlockerUtility.Models;
using System.Diagnostics;
using System.Text.Json;
using System.Text;

public class OllamaChatClientService : IChatClient
{
    private readonly HttpClient _httpClient;
    private readonly string _modelName;

    public OllamaChatClientService(Uri baseAddress, string modelName)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = baseAddress,
            Timeout = TimeSpan.FromSeconds(60)
        };
        _modelName = modelName;
    }

    public async Task<string> GetAIResponseAsync(List<ChatMessage> chatHistory)
    {
        try
        {
            var request = new
            {
                model = _modelName,
                messages = chatHistory.Select(m => new {
                    role = m.Role,
                    content = m.Content
                }),
                stream = false,
                options = new
                {
                    temperature = 0.7,
                    top_p = 0.9
                }
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(request, jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync("api/chat", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"HTTP Error: {response.StatusCode}\n{errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<OllamaResponse>(responseContent, jsonOptions);

            if (jsonResponse?.Message?.Content == null)
            {
                throw new InvalidOperationException("Invalid response format from Ollama");
            }

            return RemoveMarkdown(jsonResponse.Message.Content);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AI Request Error: {ex}");
            return $"Ошибка: {ex.Message}";
        }
    }

    private string RemoveMarkdown(string content)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        content = content.Replace("**", "").Replace("__", ""); // Жирный
        content = content.Replace("*", "").Replace("_", "");   // Курсив
        content = content.Replace("```", "").Replace("`", ""); // Код
        content = content.Replace("#", "").Replace(">", "");   // Заголовки, цитаты
        content = content.Replace("[", "").Replace("]", "");   // Ссылки
        content = content.Replace("(", "").Replace(")", "");   // Ссылки
        content = content.Replace("---", "").Replace("--", ""); // Горизонтальные линии

        content = string.Join("\n\n", content.Split(new[] { "\n\n\n" }, StringSplitOptions.None)
                                            .Select(p => p.Trim()));

        return content.Trim();
    }

    private class OllamaResponse
    {
        public Message? Message { get; set; }
    }

    private class Message
    {
        public string? Content { get; set; }
    }
}