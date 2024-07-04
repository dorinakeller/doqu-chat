using System.Text;

public class HttpResponseResult
{
    public required string Content { get; set; }
    public int StatusCode { get; set; }
    public required string ContentType { get; set; }
}

public class HttpClientWrapper(string baseUrl, string? apiKey = null)
{
    private readonly string _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl), "Base URL must be provided.");
    private readonly string _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey), "API key must be provided.");

    public async Task<HttpResponseResult> GetAsync(string relativeUrl)
    {
        using var httpClient = CreateHttpClient();

        var request = new HttpRequestMessage(HttpMethod.Get, CombineUrl(_baseUrl, relativeUrl));
        AddAuthorizationHeader(request);

        var response = await httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        return new HttpResponseResult
        {
            Content = content,
            StatusCode = (int)response.StatusCode,
            ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json"
        };
    }

    public async Task<HttpResponseResult> PostAsync(string relativeUrl, string content, string contentType = "application/json")
    {
        using var httpClient = CreateHttpClient();

        var request = new HttpRequestMessage(HttpMethod.Post, CombineUrl(_baseUrl, relativeUrl));
        AddAuthorizationHeader(request);

        request.Content = new StringContent(content, Encoding.UTF8, contentType);

        var response = await httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        return new HttpResponseResult
        {
            Content = responseContent,
            StatusCode = (int)response.StatusCode,
            ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json"
        };
    }

    public async Task<HttpResponseResult> PatchAsync(string relativeUrl, string content, string contentType = "application/json")
    {
        using var httpClient = CreateHttpClient();

        var request = new HttpRequestMessage(HttpMethod.Patch, CombineUrl(_baseUrl, relativeUrl));
        AddAuthorizationHeader(request);

        request.Content = new StringContent(content, Encoding.UTF8, contentType);

        var response = await httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        return new HttpResponseResult
        {
            Content = responseContent,
            StatusCode = (int)response.StatusCode,
            ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json"
        };
    }

    public async Task<HttpResponseResult> PutAsync(string relativeUrl, string content, string contentType = "application/json")
    {
        using var httpClient = CreateHttpClient();

        var request = new HttpRequestMessage(HttpMethod.Put, CombineUrl(_baseUrl, relativeUrl));
        AddAuthorizationHeader(request);

        request.Content = new StringContent(content, Encoding.UTF8, contentType);

        var response = await httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        return new HttpResponseResult
        {
            Content = responseContent,
            StatusCode = (int)response.StatusCode,
            ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json"
        };
    }

    public async Task<HttpResponseResult> DeleteAsync(string relativeUrl)
    {
        using var httpClient = CreateHttpClient();

        var request = new HttpRequestMessage(HttpMethod.Delete, CombineUrl(_baseUrl, relativeUrl));
        AddAuthorizationHeader(request);

        var response = await httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        return new HttpResponseResult
        {
            Content = responseContent,
            StatusCode = (int)response.StatusCode,
            ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json"
        };
    }

    private static HttpClient CreateHttpClient()
    {
        var httpClient = new HttpClient();
        // You can configure HttpClient settings here if needed
        return httpClient;
    }

    private void AddAuthorizationHeader(HttpRequestMessage request)
    {
        request.Headers.Add("Authorization", $"Bearer {_apiKey}");

    }

    private static string CombineUrl(string baseUrl, string relativeUrl)
    {
        return baseUrl.TrimEnd('/') + "/" + relativeUrl.TrimStart('/');
    }
}

