using Azure.Messaging.WebPubSub.Clients;
using OpenAI.Chat;
using Request.DTOs;
using Response.DTOs;

namespace GenerativeAI
{
    public interface IAzureOpenAIGPT
    {
        public Task<WebPubSubClient> EstablishWebSocket(string chatGroupId);

        public Task<ChatResponseDTO> Chat(string userMessage);

        public Task<string> GenerateTitle(string userMessage, string completeMessageContent, ChatClient chatClient, ChatCompletionOptions completionOptions);
    }
}