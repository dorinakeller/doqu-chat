using Microsoft.OpenApi.Models;
using System.Text.Json;
using DotNetEnv;
using GenerativeAI;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

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
            Console.WriteLine("here");
            using var reader = new StreamReader(context.Request.Body);
            var requestBody = await reader.ReadToEndAsync();
            var chatRequest = JsonSerializer.Deserialize<ChatRequest>(requestBody) ?? throw new Exception("Incoming body is null!");
            if (string.IsNullOrWhiteSpace(chatRequest.client_id) || string.IsNullOrWhiteSpace(chatRequest.chat_group_id) || string.IsNullOrWhiteSpace(chatRequest.message))
            {
                throw new Exception("Incoming JSON key-value is null!");
            }

            var clientId = chatRequest.client_id;
            var chatGroupId = chatRequest.chat_group_id;
            var message = chatRequest.message;

            var url = $"{BackendUrl}/fetchcontext?clientId={clientId}&chatGroupId={chatGroupId}";

            // Fetch context from backend
            var client = httpClientFactory.CreateClient();
            var fetchContextResponse = await client.GetAsync(url);
            if (fetchContextResponse.IsSuccessStatusCode)
            {
                var responseContent = await fetchContextResponse.Content.ReadAsStringAsync();
                Console.WriteLine("Response Content: " + responseContent);

                try
                {
                    var contextData = JsonSerializer.Deserialize<FetchContextResponse>(responseContent);
                    Console.WriteLine(contextData.system_prompt);
                    // Console.WriteLine("Chat Model Deployment Name: " + contextData.ChatModel?.DeploymentName ?? "Not available");
                    // foreach (var chatMessage in contextData.ChatHistory ?? new List<ChatMessage>())
                    // {
                    //     Console.WriteLine($"Message from {chatMessage.SentBy}: {chatMessage.Message} at {chatMessage.Timestamp}");
                    // }
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



            int maxTokens = 4096;
            float temperature = 0.9f;
            float frequencyPenalty = 0.0f;
            float presencePenalty = 0.6f;

            // Create an instance of AzureOpenAIGPT
            var azureOpenAIGPT = new AzureOpenAIGPT(maxTokens, temperature, frequencyPenalty, presencePenalty);

            // Use the instance to chat
            string messageId = Guid.NewGuid().ToString();

            string response = await azureOpenAIGPT.Chat(messageId, message, chatGroupId);
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize("Process finished"));
        }
        catch (Exception ex)
        {
            Console.WriteLine("error");
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
        }
    }
);

app.Run();
internal class ChatRequest
{
    public string? client_id { get; set; }
    public string? chat_group_id { get; set; }
    public string? message { get; set; }
}

// public class ChatMessage
// {
//     public string? sent_by { get; set; }
//     public string? message { get; set; }
//     public string? timestamp { get; set; }
//     public string? messageId { get; set; }
// }

// public class ChatModel
// {
//     public string? deployment_name { get; set; }
//     public string? endpoint { get; set; }
//     public string? api_version { get; set; }
// }

// public class FetchContextResponse
// {
//     public List<ChatMessage> chat_history { get; set; }
//     public string system_prompt { get; set; }
//     public ChatModel chat_model { get; set; }
// }

