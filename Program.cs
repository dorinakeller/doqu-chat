using Microsoft.OpenApi.Models;
using System.Text.Json;
using DotNetEnv;
using GenerativeAI;

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
            if (string.IsNullOrWhiteSpace(chatRequest.client_id) || string.IsNullOrWhiteSpace(chatRequest.chat_group_id) || string.IsNullOrWhiteSpace(chatRequest.message))
            {
                throw new Exception("Incoming JSON key-value is null!");
            }

            var clientId = chatRequest.client_id;
            var chatGroupId = chatRequest.chat_group_id;
            var message = chatRequest.message;

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

