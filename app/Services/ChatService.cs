using System.Text;
using System.Text.Json;
using Response.DTOs;

public static class ChatService
{
    public static StringContent CreateChatResponseBody(string clientId, string userMessage, ChatResponseDTO chatResponse)
    {
        // Prepare the chat response body
        var dataChat = new List<Dictionary<string, string>>
        {
            new() { { "sent_by", clientId }, { "message", userMessage } },
            new() { { "sent_by", "gpt4o@gpt.gpt" }, { "message", chatResponse.CompleteMessageContent } }
        };

        var data = new Dictionary<string, object>
        {
            { "chat_history", dataChat },
            { "request_type", "title-history" }
        };

        if (!string.IsNullOrEmpty(chatResponse.Title))
        {
            data.Add("title", chatResponse.Title);
        }

        var body = new Dictionary<string, object>
        {
            { "data", data }
        };

        var content = JsonSerializer.Serialize(body);
        return new StringContent(content, Encoding.UTF8, "application/json");
    }
}
