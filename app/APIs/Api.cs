namespace Microservice.Endpoints
{
    public static class ChatEndpoints
    {
        public static void MapChatEndpoints(this IEndpointRouteBuilder app)
        {
            HealthEndpoint.MapHealthEndpoint(app);
            InvokeEndpoint.MapInvokeEndpoint(app);
        }
    }
}