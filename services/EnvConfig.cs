public class AppConfiguration
{
    public string BackendUrl { get; }
    public string ApiKey { get; }

    public AppConfiguration()
    {
        // Read and validate required environment variables
        BackendUrl = RequireEnvironmentVariable("BACKEND_URL", "Base URL must be provided.");
        ApiKey = RequireEnvironmentVariable("API_KEY", "API KEY must be provided.");
    }

    private static string RequireEnvironmentVariable(string variableName, string errorMessage)
    {
        var value = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentNullException(variableName, errorMessage);
        }
        return value;
    }
}