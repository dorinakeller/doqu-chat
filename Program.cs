using Microsoft.OpenApi.Models;
using System.Text.Json;
using DotNetEnv;
using GenerativeAI;
using System.Text;

using ChatInterfaces;

Env.Load(".env.local");
var config = new AppConfiguration();


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
builder.Services.AddTransient<HttpClientWrapper>(provider =>
{
    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
    return new HttpClientWrapper(httpClientFactory, config.BackendUrl, config.ApiKey, config.Email);
});
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



// Configure middleware and endpoints
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Chat MicroService API v1.0");
    });
}


app.MapGet("/health", async (HttpContext context, HttpClientWrapper httpClientWrapper) =>
{
    try
    {
        // Create an instance of HttpClientWrapper using the provided base URL and API key
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

app.MapPost("/invoke", async (HttpContext context, HttpClientWrapper httpClientWrapper, ILogger<Program> logger) =>
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
            Console.WriteLine("Response ClientId: " + chatRequest.ClientId);
            Console.WriteLine("Response ChatGroupId: " + chatRequest.ChatGroupId);
            Console.WriteLine("Response Message: " + chatRequest.Message);
            throw new Exception("Incoming JSON key-value is null!");
        }

        var clientId = chatRequest.ClientId;
        var chatGroupId = chatRequest.ChatGroupId;
        var message = chatRequest.Message;

        // Fetch context from backend

        var fetchContextResponse = await httpClientWrapper.GetAsync($"chat/{chatGroupId}/?request_type=microservice-data");
        if (fetchContextResponse.StatusCode == 200)
        {
            try
            {
                var contextData = JsonSerializer.Deserialize<FetchContextResponse>(fetchContextResponse.Content) ?? throw new Exception("Incoming body is null!");

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
                var azureOpenAIGPT = new AzureOpenAIGPT(maxTokens, temperature, frequencyPenalty, presencePenalty, chatGroupId);
                var chatResponse = await azureOpenAIGPT.Chat(message, chatGroupId, contextData);

                Console.WriteLine("Chat response: " + chatResponse.CompleteMessageContent);
                Console.WriteLine("Chat title: " + chatResponse.Title);

                // Send POST request using HttpClient
                string messageId = Guid.NewGuid().ToString();
                var serializedData = CreateChatResponseBody(chatRequest.ClientId, chatRequest.Message, chatResponse);
                var postResponse = await httpClientWrapper.PatchAsync($"chat/{chatGroupId}/", serializedData);
                // request type-ot a data-n belül küldömx


                if (postResponse.StatusCode != 204)
                {
                    throw new Exception($"Error sending chat response: {postResponse.StatusCode}");
                }


            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine("JSON Deserialization Error: " + jsonEx.Message);
            }
        }
        else
        {
            throw new Exception($"{fetchContextResponse.StatusCode}");
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

static StringContent CreateChatResponseBody(string clientId, string userMessage, ChatResponse chatResponse)
// Prepare the chat response body
{
    var dataChat = new List<Dictionary<string, string>>
    {
        new() { { "sent_by", clientId }, { "message", userMessage } },
        new() { { "sent_by", "gpt4o@gpt.gpt" }, { "message", chatResponse.CompleteMessageContent } }
    };

    var data = new Dictionary<string, object>
    {
        { "chat_history", dataChat },
    };
    data.Add("request_type", "title-history");

    if (!string.IsNullOrEmpty(chatResponse.Title))
    {
        data.Add("title", chatResponse.Title);
    }

    var body = new Dictionary<string, object>
        {
            { "data", data }
        };

    var content = JsonSerializer.Serialize(body);
    var stringContent = new StringContent(content, Encoding.UTF8, "application/json");

    return stringContent;
}
