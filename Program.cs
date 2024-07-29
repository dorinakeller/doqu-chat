using DotNetEnv;
using Microservice.Endpoints;

// Load Env
Env.Load(".env.local");

// Configure services
var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureServices();

// Build the app
var app = builder.Build();

// Enable CORS
app.UseCors("AllowLocalhost3000");

// Configure Swagger for API documentation
app.ConfigureSwagger();

// Set endpoints
app.MapChatEndpoints();

app.Run();