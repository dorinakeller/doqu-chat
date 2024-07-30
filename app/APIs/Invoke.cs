using System.Text.Json;
using GenerativeAI;
using Request.DTOs;

namespace Microservice.Endpoints
{
    public static class InvokeEndpoint
    {
        public static void MapInvokeEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapPost("/invoke", async (ChatRequestDTO chatRequest, HttpContext context, HttpClientWrapper httpClientWrapper, ILogger<Program> logger) =>
            {
                try
                {
                    // Request body extraction
                    var (clientId, chatGroupId, message) = (chatRequest.ClientId, chatRequest.ChatGroupId, chatRequest.Message);

                    // Fetch chat context data from the backend
                    var fetchContextResponse = await httpClientWrapper.GetAsync($"chat/{chatGroupId}/?request_type=microservice-data");
                    if (fetchContextResponse.StatusCode != 200)
                        throw new Exception($"Error fetching context: {fetchContextResponse.StatusCode}");

                    // Serialize GET data
                    var contextData = JsonSerializer.Deserialize<FetchContextDTO>(fetchContextResponse.Content)
                        ?? throw new Exception("Incoming body is null!");

                    // Call Azure OpenAI and stream data
                    var azureOpenAIGPT = new AzureOpenAIGPT(contextData, chatGroupId);
                    var chatResponse = await azureOpenAIGPT.Chat(message);

                    // PATCH backend chathistory with the new message-answer pair
                    var serializedData = ChatService.CreateChatResponseBody(contextData.serviceId, message, chatResponse);
                    var patchResponse = await httpClientWrapper.PatchAsync($"chat/{chatGroupId}/", serializedData);
                    if (patchResponse.StatusCode != 204)
                        throw new Exception($"Error sending chat response: {patchResponse.StatusCode}");

                    return Results.Ok();
                }
                catch (JsonException jsonEx)
                {
                    logger.LogError(jsonEx, "JSON Deserialization Error");
                    return Results.Problem(detail: "Invalid JSON format", statusCode: StatusCodes.Status400BadRequest);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred");
                    return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
        }
    }
}
