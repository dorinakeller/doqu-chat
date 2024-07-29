using System.Text.Json;

namespace Microservice.Endpoints
{
    public static class HealthEndpoint
    {
        public static void MapHealthEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapGet("/health", async (HttpContext context, HttpClientWrapper httpClientWrapper) =>
            {
                try
                {
                    var response = await httpClientWrapper.GetAsync("health");
                    context.Response.StatusCode = response.StatusCode;
                    context.Response.ContentType = response.ContentType;

                    await context.Response.WriteAsync(response.Content);
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize($"The following error occurred while processing your request: {ex}"));
                }
            })
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status500InternalServerError);
        }
    }
}
