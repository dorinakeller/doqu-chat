using OpenAI.Chat;
using Azure.AI.OpenAI;
using Azure;
using System.ClientModel;

namespace GenerativeAI;

public class AzureOpenAIGPT(int maxTokens, float temperature, float frequencyPenalty, float presencePenalty)
{
    private static readonly string Key = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")
         ?? throw new InvalidOperationException("Environment variable AZURE_OPENAI_API_KEY is not set.");
    private static readonly string Endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
        ?? throw new InvalidOperationException("Environment variable AZURE_OPENAI_ENDPOINT is not set.");
    private static readonly string DeploymentOrModelName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME")
        ?? throw new InvalidOperationException("Environment variable AZURE_OPENAI_DEPLOYMENT_NAME is not set.");

    private readonly int maxTokens = maxTokens;
    private readonly float temperature = temperature;
    private readonly float frequencyPenalty = frequencyPenalty;
    private readonly float presencePenalty = presencePenalty;

    public async Task<string> Chat(string prompt, string message)
    {
        AzureOpenAIClient azureClient = new(
                    new Uri(Endpoint),
                    new AzureKeyCredential(Key)
                );
        ChatClient chatClient = azureClient.GetChatClient(DeploymentOrModelName);

        var messages = new List<ChatMessage>
        {new SystemChatMessage(prompt),
            new UserChatMessage(message)
        };

        var completionOptions = new ChatCompletionOptions
        {
            MaxTokens = maxTokens,
            Temperature = temperature,
            FrequencyPenalty = frequencyPenalty,
            PresencePenalty = presencePenalty,
        };
        AsyncResultCollection<StreamingChatCompletionUpdate> updates = chatClient.CompleteChatStreamingAsync(messages, completionOptions);

        await foreach (StreamingChatCompletionUpdate update in updates)
        {
            foreach (ChatMessageContentPart updatePart in update.ContentUpdate)
            {
                Console.Write(updatePart.Text);
            }
        }

        // var response = await chatClient.CompleteChatAsync(messages, completionOptions);
        return "asd";
        // return response.Value.Content[0].Text;
    }
}

