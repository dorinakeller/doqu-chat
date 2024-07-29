
using System.Text.Json.Serialization;


namespace Request.DTOs
{
    public class ChatRequestDTO

    {
        [JsonPropertyName("clientId")]
        public required string ClientId { get; set; }

        [JsonPropertyName("chatGroupId")]
        public required string ChatGroupId { get; set; }

        [JsonPropertyName("message")]
        public required string Message { get; set; }
    }

    public class ChatHistoryDTO
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

    public class ChatModelDTO
    {
        [JsonPropertyName("deployment_name")]
        public required string DeploymentName { get; set; }

        [JsonPropertyName("endpoint")]
        public required string Endpoint { get; set; }

        [JsonPropertyName("api_version")]
        public required string ApiVersion { get; set; }

        [JsonPropertyName("max_tokens")]
        public required int max_tokens { get; set; }

        [JsonPropertyName("temperature")]
        public required float temperature { get; set; }

        [JsonPropertyName("frequency_penalty")]
        public required float frequency_penalty { get; set; }

        [JsonPropertyName("presence_penalty")]
        public required float presence_penalty { get; set; }
    }

    public class FetchContextDTO
    {
        [JsonPropertyName("chat_history")]
        public required List<ChatHistoryDTO> ChatHistory { get; set; }

        [JsonPropertyName("system_prompt")]
        public required string SystemPrompt { get; set; }

        [JsonPropertyName("chat_model")]
        public required ChatModelDTO ChatModel { get; set; }

        [JsonPropertyName("service_id")]
        public required string serviceId { get; set; }
    }

}
