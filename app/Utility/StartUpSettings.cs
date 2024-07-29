using Microsoft.OpenApi.Models;

public static class StartupExtensions
{
    public static void ConfigureServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Chat MicroService API",
                Description = "Endpoints",
                Version = "v1.0"
            });
        });

        services.AddHttpClient();
        services.AddTransient<HttpClientWrapper>(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var config = new AppConfiguration();

            return new HttpClientWrapper(httpClientFactory, config.BackendUrl, config.ApiKey, config.Email);
        });

        services.AddCors(options =>
        {
            options.AddPolicy("AllowLocalhost3000", builder =>
            {
                builder.WithOrigins("http://localhost:3000")
                       .AllowAnyHeader()
                       .AllowAnyMethod();
            });
        });

        services.AddLogging(loggingBuilder => loggingBuilder.AddConsole());
    }

    public static void ConfigureSwagger(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Chat MicroService API v1.0");
            });
        }
    }
}