using Microsoft.OpenApi.Models;
using System.Text.Json;
using DotNetEnv;
using GenerativeAI;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

Env.Load(".env.local");

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Chat MicroService API",
        Description = "Endpoints",
        Version = "v1.0"
    });
});
builder.Services.AddHttpClient();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost3000",
        builder =>
        {
            builder.WithOrigins("http://localhost:3000")
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});
builder.Logging.AddConsole();

var app = builder.Build();
app.UseCors("AllowLocalhost3000");

var config = new AppConfiguration();
var BackendUrl = config.BackendUrl;
var apiKey = config.ApiKey;

// Configure middleware and endpoints
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Chat MicroService API v1.0");
    });
}

app.MapGet("/fetchcontext", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        // Create an instance of HttpClientWrapper using the provided base URL and API key
        var httpClientWrapper = new HttpClientWrapper(BackendUrl, apiKey: apiKey);

        // Perform the health check request
        var response = await httpClientWrapper.GetAsync("fetchcontext");

        // Set the response status code and content type based on the response from the HttpClientWrapper
        context.Response.StatusCode = response.StatusCode;
        context.Response.ContentType = response.ContentType;

        // Write the response content to the HTTP response
        await context.Response.WriteAsync(response.Content);
    }
    catch (Exception ex)
    {
        // Set the response status code and content type for the error
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        // Write the error message to the HTTP response
        await context.Response.WriteAsync(JsonSerializer.Serialize("The following error occurred while processing your request. " + ex));
    }
}
);

app.MapGet("/health", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        // Create an instance of HttpClientWrapper using the provided base URL and API key
        var httpClientWrapper = new HttpClientWrapper(BackendUrl, apiKey: apiKey);

        // Perform the health check request
        var response = await httpClientWrapper.GetAsync("health");

        // Set the response status code and content type based on the response from the HttpClientWrapper
        context.Response.StatusCode = response.StatusCode;
        context.Response.ContentType = response.ContentType;

        // Write the response content to the HTTP response
        await context.Response.WriteAsync(response.Content);
    }
    catch (Exception ex)
    {
        // Set the response status code and content type for the error
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        // Write the error message to the HTTP response
        await context.Response.WriteAsync(JsonSerializer.Serialize("The following error occurred while processing your request. " + ex));
    }
}
);

app.MapPost("/invoke", async (HttpContext context, IHttpClientFactory httpClientFactory, ILogger<Program> logger) =>
{
    try
    {
        using var reader = new StreamReader(context.Request.Body);
        var requestBody = await reader.ReadToEndAsync();
        var chatRequest = JsonSerializer.Deserialize<ChatRequest>(requestBody) ?? throw new Exception("Incoming body is null!");

        if (string.IsNullOrWhiteSpace(chatRequest.ClientId) ||
            string.IsNullOrWhiteSpace(chatRequest.ChatGroupId) ||
            string.IsNullOrWhiteSpace(chatRequest.Message))
        {
            throw new Exception("Incoming JSON key-value is null!");
        }

        var clientId = chatRequest.ClientId;
        var chatGroupId = chatRequest.ChatGroupId;
        var message = chatRequest.Message;

        var url = $"{BackendUrl}/fetchcontext?clientId={clientId}&chatGroupId={chatGroupId}";

        // Fetch context from backend
        var client = httpClientFactory.CreateClient();
        var fetchContextResponse = await client.GetAsync(url);
        if (fetchContextResponse.IsSuccessStatusCode)
        {
            var responseContent = await fetchContextResponse.Content.ReadAsStringAsync();
            Console.WriteLine("------------------------- ");
            Console.WriteLine("Response Content: " + responseContent);

            try
            {
                var contextData = JsonSerializer.Deserialize<FetchContextResponse>(responseContent);

                if (contextData == null)
                {
                    Console.WriteLine("contextData is null after deserialization.");
                }
                else
                {
                    // Log each property to verify deserialization
                    Console.WriteLine("Deserialized contextData object:");
                    Console.WriteLine("SystemPrompt: " + contextData.SystemPrompt);
                    Console.WriteLine("ChatModel Deployment Name: " + contextData.ChatModel.DeploymentName);
                    Console.WriteLine("ChatModel Endpoint: " + contextData.ChatModel.Endpoint);
                    Console.WriteLine("ChatModel ApiVersion: " + contextData.ChatModel.ApiVersion);
                    Console.WriteLine("ChatHistory count: " + contextData.ChatHistory.Count);

                    int maxTokens = 4096;
                    float temperature = 0.9f;
                    float frequencyPenalty = 0.0f;
                    float presencePenalty = 0.6f;

                    // Create an instance of AzureOpenAIGPT
                    var azureOpenAIGPT = new AzureOpenAIGPT(maxTokens, temperature, frequencyPenalty, presencePenalty);
                    // Use the instance to chat
                    string messageId = Guid.NewGuid().ToString();

                    var chatResponse = await azureOpenAIGPT.Chat(messageId, message, chatGroupId, contextData);

                    Console.WriteLine("Chat response: " + chatResponse.CompleteMessageContent);
                    Console.WriteLine("Chat title: " + chatResponse.Title);

                    var body = CreateChatResponseBody(chatRequest.ClientId, chatRequest.Message, chatResponse);



                    // Construct the endpoint URL
                    var postUrl = $"{BackendUrl}/chat/{chatGroupId}/patch/";

                    // Send POST request using HttpClient
                    // var client = httpClientFactory.CreateClient();
                    var content = JsonSerializer.Serialize(body);
                    var stringContent = new StringContent(content, Encoding.UTF8, "application/json");
                    var postResponse = await client.PatchAsync(postUrl, stringContent);

                    if (postResponse.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Successfully sent chat response to backend.");
                    }
                    else
                    {
                        Console.WriteLine("Error sending chat response: " + postResponse.StatusCode);
                    }
                }
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine("JSON Deserialization Error: " + jsonEx.Message);
            }
        }
        else
        {
            Console.WriteLine("Error fetching context: " + fetchContextResponse.StatusCode);
        }

        context.Response.StatusCode = 200;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize("Process finished"));
    }
    catch (Exception ex)
    {
        Console.WriteLine("error: " + ex.Message);
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
    }
});

app.Run();

Dictionary<string, object> CreateChatResponseBody(string clientId, string userMessage, ChatResponse chatResponse)
// Prepare the chat response body
{
    var chatHistory = new List<Dictionary<string, string>>
    {
        new Dictionary<string, string> { { "sent_by", clientId }, { "message", userMessage }, { "messageId", null } },
        new Dictionary<string, string> { { "sent_by", "gpt4o@gpt.gpt" }, { "message", chatResponse.CompleteMessageContent }, { "messageId", null } }
    };

    var data = new Dictionary<string, object>
    {
        { "chat_history", chatHistory }
    };

    if (!string.IsNullOrEmpty(chatResponse.Title))
    {
        data.Add("title", chatResponse.Title);
    }

    return new Dictionary<string, object>
    {
        { "data", data }
    };
}


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
    public string SentBy { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; }

    [JsonPropertyName("messageId")]
    public string MessageId { get; set; }
}

public class ChatModel
{
    [JsonPropertyName("deployment_name")]
    public string DeploymentName { get; set; }

    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; }

    [JsonPropertyName("api_version")]
    public string ApiVersion { get; set; }
}

public class FetchContextResponse
{
    [JsonPropertyName("chat_history")]
    public List<ChatHistory> ChatHistory { get; set; }

    [JsonPropertyName("system_prompt")]
    public string SystemPrompt { get; set; }

    [JsonPropertyName("chat_model")]
    public ChatModel ChatModel { get; set; }
}

public class ChatResponse
{
    public string CompleteMessageContent { get; set; }
    public string Title { get; set; }
}