
using System.Text.Json.Serialization;

namespace ChatInterfaces
{
    public class ChatRequest
    {
        [JsonPropertyName("client_id")]
        public string? ClientId { get; set; }

        [JsonPropertyName("chat_group_id")]
        public string? ChatGroupId { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    public class ChatHistory
    {
        [JsonPropertyName("sent_by")]
        public required string SentBy { get; set; }

        [JsonPropertyName("message")]
        public required string Message { get; set; }

        [JsonPropertyName("timestamp")]
        public required string Timestamp { get; set; }

        [JsonPropertyName("messageId")]
        public required string MessageId { get; set; }
    }

    public class ChatModel
    {
        [JsonPropertyName("deployment_name")]
        public required string DeploymentName { get; set; }

        [JsonPropertyName("endpoint")]
        public required string Endpoint { get; set; }

        [JsonPropertyName("api_version")]
        public required string ApiVersion { get; set; }
    }

    public class FetchContextResponse
    {
        [JsonPropertyName("chat_history")]
        public required List<ChatHistory> ChatHistory { get; set; }

        [JsonPropertyName("system_prompt")]
        public required string SystemPrompt { get; set; }

        [JsonPropertyName("chat_model")]
        public required ChatModel ChatModel { get; set; }
    }

    public class ChatResponse
    {
        public required string CompleteMessageContent { get; set; }
        public required string? Title { get; set; }

    }
}
