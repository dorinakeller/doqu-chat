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
        Version = "v1"
    });
});
builder.Services.AddHttpClient();
builder.Logging.AddConsole();

var app = builder.Build();

var config = new AppConfiguration();
var chatBaseUrl = config.ChatBaseUrl;
var apiKey = config.ApiKey;

// Configure middleware and endpoints
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Microservice");
    });
}

app.MapGet("/health", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
    {
        var httpClientWrapper = new HttpClientWrapper(chatBaseUrl, apiKey: apiKey);
        var response = await httpClientWrapper.GetAsync("health");

        context.Response.StatusCode = response.StatusCode;
        context.Response.ContentType = response.ContentType;
        await context.Response.WriteAsync(response.Content);
    }
);

app.MapPost("/invoke", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
    {
        int maxTokens = 4096;
        float temperature = 0.9f;
        float frequencyPenalty = 0.0f;
        float presencePenalty = 0.6f;

        // Create an instance of AzureOpenAIGPT
        var azureOpenAIGPT = new AzureOpenAIGPT(maxTokens, temperature, frequencyPenalty, presencePenalty);

        // Use the instance to chat
        string prompt = "You are a funny chatgpt who likes to tell jokes!";
        string message = "Write a long py script and do that in .net as well and compare it!";
        string response = await azureOpenAIGPT.Chat(prompt, message);

        // Log response details for debugging
        Console.Write("OpenAI " + response);

        context.Response.StatusCode = 200;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
);

app.Run();
