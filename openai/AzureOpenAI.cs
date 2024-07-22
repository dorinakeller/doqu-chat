
using Azure;
using Azure.AI.OpenAI;
using Azure.Messaging.WebPubSub;
using Azure.Messaging.WebPubSub.Clients;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;
using Websocket.Client;

namespace GenerativeAI;

public class AzureOpenAIGPT(int maxTokens, float temperature, float frequencyPenalty, float presencePenalty)
{
    private static readonly string Key = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")
         ?? throw new InvalidOperationException("Environment variable AZURE_OPENAI_API_KEY is not set.");
    private static readonly string Endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
        ?? throw new InvalidOperationException("Environment variable AZURE_OPENAI_ENDPOINT is not set.");
    private static readonly string DeploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME")
        ?? throw new InvalidOperationException("Environment variable AZURE_OPENAI_DEPLOYMENT_NAME is not set.");
    private static readonly string WebPubSubEndpoint = Environment.GetEnvironmentVariable("AZURE_WEB_PUBSUB_ENDPOINT")
    ?? throw new InvalidOperationException("Environment variable AZURE_WEB_PUBSUB_ENDPOINT is not set.");
    private static readonly string WebPubSubHub = Environment.GetEnvironmentVariable("AZURE_WEB_PUBSUB_HUB")
    ?? throw new InvalidOperationException("Environment variable AZURE_WEB_PUBSUB_HUB is not set.");

    private readonly int maxTokens = maxTokens;
    private readonly float temperature = temperature;
    private readonly float frequencyPenalty = frequencyPenalty;
    private readonly float presencePenalty = presencePenalty;

    // public async Task<WebPubSubClient> EstablishWebSocket()
    // {
    //     var serviceClient = new WebPubSubServiceClient(WebPubSubEndpoint, WebPubSubHub);

    //     DateTimeOffset expiration = DateTimeOffset.UtcNow.AddMinutes(5);

    //     var url = serviceClient.GetClientAccessUri(
    //         expiresAt: expiration,
    //         userId: "gpt_model",
    //         roles: ["webpubsub.sendToGroup.chat", "webpubsub.joinLeaveGroup.chat"]
    //     ).AbsoluteUri;

    //     var wsClient = new WebPubSubClient(new Uri(url));
    //     try
    //     {
    //         await wsClient.StartAsync();
    //         await wsClient.JoinGroupAsync("chat");
    //         return wsClient;
    //     }
    //     catch (Exception ex)
    //     {
    //         Console.WriteLine($"Error establishing WebSocket connection: {ex.Message}");
    //         throw;
    //     }

    // }

    public async Task<string> Chat(string messageId, string userMessage, string chat_group_id, FetchContextResponse contextData)
    {
        AzureOpenAIClient azureClient = new(
                    new Uri(contextData.ChatModel.Endpoint),
                    new AzureKeyCredential(Key)
                );
        ChatClient chatClient = azureClient.GetChatClient(contextData.ChatModel.DeploymentName);
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(contextData.SystemPrompt),
            new UserChatMessage(userMessage)
        };

        var completionOptions = new ChatCompletionOptions
        {
            MaxTokens = maxTokens,
            Temperature = temperature,
            FrequencyPenalty = frequencyPenalty,
            PresencePenalty = presencePenalty,
        };

        AsyncResultCollection<StreamingChatCompletionUpdate> updates = chatClient.CompleteChatStreamingAsync(messages, completionOptions);
        string completeMessageContent = "";
        await foreach (StreamingChatCompletionUpdate update in updates)
        {
            foreach (ChatMessageContentPart updatePart in update.ContentUpdate)
            {
                Console.WriteLine(updatePart.Text);
                completeMessageContent += updatePart.Text;
            }
        }
        // WebPubSubClient client = await EstablishWebSocket();

        // await foreach (StreamingChatCompletionUpdate update in updates)
        // {
        //     foreach (ChatMessageContentPart updatePart in update.ContentUpdate)
        //     {

        //         Console.Write(updatePart.Text);
        //         string message = updatePart.Text;

        //         await client.SendToGroupAsync(
        //             chat_group_id,
        //             new BinaryData(JsonSerializer.Serialize(new { messageId, message, from = "microservice", error = false })),
        //             WebPubSubDataType.Json,
        //             ackId: null,  // No need to specify ackId if you want to wait for acknowledgment
        //             noEcho: true, // Optional: Set noEcho to true if you don't want the message echoed back to sender
        //             fireAndForget: true, // Ensure fireAndForget is false to wait for acknowledgment
        //             CancellationToken.None
        //         );

        //     }
        // }

        // await client.StopAsync();

        // var response = await chatClient.CompleteChatAsync(messages, completionOptions);
        return completeMessageContent;
        // return response.Value.Content[0].Text;
    }
}