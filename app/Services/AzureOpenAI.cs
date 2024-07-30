
using Azure;
using Azure.AI.OpenAI;
using Azure.Messaging.WebPubSub;
using Azure.Messaging.WebPubSub.Clients;

using OpenAI.Chat;
using Request.DTOs;
using Response.DTOs;
using System.ClientModel;
using System.Text.Json;

namespace GenerativeAI;

public class AzureOpenAIGPT(FetchContextDTO contextData, string chatGroupId) : IAzureOpenAIGPT
{
    private static readonly string Key = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")
         ?? throw new InvalidOperationException("Environment variable AZURE_OPENAI_API_KEY is not set.");
    private static readonly string WebPubSubEndpoint = Environment.GetEnvironmentVariable("AZURE_WEB_PUBSUB_ENDPOINT")
        ?? throw new InvalidOperationException("Environment variable AZURE_WEB_PUBSUB_ENDPOINT is not set.");
    private static readonly string WebPubSubHub = Environment.GetEnvironmentVariable("AZURE_WEB_PUBSUB_HUB")
        ?? throw new InvalidOperationException("Environment variable AZURE_WEB_PUBSUB_HUB is not set.");


    private readonly string chatGroupId = chatGroupId;

    public async Task<ChatResponseDTO> Chat(string userMessage)
    {
        AzureOpenAIClient azureClient = new(
                    new Uri(contextData.ChatModel.Endpoint),
                    new AzureKeyCredential(Key)
                );
        ChatClient chatClient = azureClient.GetChatClient(contextData.ChatModel.DeploymentName);
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(contextData.SystemPrompt)
        };

        // Add chat history to messages
        foreach (var chatHistoryItem in contextData.ChatHistory)
        {
            if (chatHistoryItem.SentBy == "gpt4o@gpt.gpt")
            {
                messages.Add(new SystemChatMessage(chatHistoryItem.Message));
            }
            else
            {
                messages.Add(new UserChatMessage(chatHistoryItem.Message));
            }
        }

        // Add the user's message
        messages.Add(new UserChatMessage(userMessage));

        var completionOptions = new ChatCompletionOptions
        {
            MaxTokens = contextData.ChatModel.max_tokens,
            Temperature = contextData.ChatModel.temperature,
            FrequencyPenalty = contextData.ChatModel.frequency_penalty,
            PresencePenalty = contextData.ChatModel.presence_penalty,
        };

        AsyncResultCollection<StreamingChatCompletionUpdate> updates = chatClient.CompleteChatStreamingAsync(messages, completionOptions);

        WebPubSubClient client = await EstablishWebSocket(chatGroupId);

        string completeMessageContent = "";
        string messageId = Guid.NewGuid().ToString();

        await foreach (StreamingChatCompletionUpdate update in updates)
        {
            foreach (ChatMessageContentPart updatePart in update.ContentUpdate)
            {
                completeMessageContent += updatePart.Text;
                string message = updatePart.Text;

                await client.SendToGroupAsync(
                    chatGroupId,
                    new BinaryData(JsonSerializer.Serialize(new { messageId, message, from = "microservice", error = false })),
                    WebPubSubDataType.Json,
                    ackId: null,  // No need to specify ackId if you want to wait for acknowledgment
                    noEcho: true, // Optional: Set noEcho to true if you don't want the message echoed back to sender
                    fireAndForget: true, // Ensure fireAndForget is false to wait for acknowledgment
                    CancellationToken.None
                );

            }
        }

        await client.StopAsync();

        string? title = null;

        // Generate title only if chat history is empty
        if (contextData.ChatHistory.Count == 0 || contextData.ChatHistory.Count % 8 == 0)
        {
            title = await GenerateTitle(userMessage, completeMessageContent, chatClient, completionOptions);
        }

        return new ChatResponseDTO
        {
            CompleteMessageContent = completeMessageContent,
            Title = title,
        };

    }
    public async Task<WebPubSubClient> EstablishWebSocket(string chatGroupId)
    {
        var serviceClient = new WebPubSubServiceClient(WebPubSubEndpoint, WebPubSubHub);

        DateTimeOffset expiration = DateTimeOffset.UtcNow.AddMinutes(5);

        var url = serviceClient.GetClientAccessUri(
            expiresAt: expiration,
            userId: "gpt_model",
            roles: [$"webpubsub.sendToGroup.{chatGroupId}", $"webpubsub.joinLeaveGroup.{chatGroupId}"]
        ).AbsoluteUri;

        var wsClient = new WebPubSubClient(new Uri(url));
        try
        {
            await wsClient.StartAsync();
            await wsClient.JoinGroupAsync(chatGroupId);
            return wsClient;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error establishing WebSocket connection: {ex.Message}");
        }

    }


    public async Task<string> GenerateTitle(string userMessage, string completeMessageContent, ChatClient chatClient, ChatCompletionOptions completionOptions)
    {
        var messagesTitle = new List<ChatMessage>
        {
            new UserChatMessage(userMessage),
            new SystemChatMessage(completeMessageContent),
            new SystemChatMessage("Generate a title for the above conversation, which summarize the previous conversation so far! Keep it concise and less than 255 characters long, without emojis and formatting."),
        };

        var titleResponse = await chatClient.CompleteChatAsync(messagesTitle, completionOptions);
        return titleResponse.Value.Content[0].Text;
    }

}


