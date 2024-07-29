
using System.Text;


public class HttpResponseResult
{
    public required string Content { get; set; }
    public int StatusCode { get; set; }
    public required string ContentType { get; set; }
}

public class HttpClientWrapper
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _baseUrl;
    private readonly string _apiKey;
    private readonly string _email;

    public HttpClientWrapper(IHttpClientFactory httpClientFactory, string baseUrl, string apiKey, string email)
    {
        _httpClientFactory = httpClientFactory;
        _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl), "Base URL must be provided.");
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey), "API key must be provided.");
        _email = email ?? throw new ArgumentNullException(nameof(email), "Email must be provided.");
    }

    public async Task<HttpResponseResult> GetAsync(string relativeUrl)
    {
        var httpClient = _httpClientFactory.CreateClient();
        Console.WriteLine("Chat _baseUrl: " + _baseUrl);
        Console.WriteLine("Chat relativeUrl: " + relativeUrl);
        Console.WriteLine("Chat url: " + CombineUrl(_baseUrl, relativeUrl));
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
        var httpClient = _httpClientFactory.CreateClient();
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

    public async Task<HttpResponseResult> PatchAsync(string relativeUrl, StringContent content, string contentType = "application/json")
    {
        var httpClient = _httpClientFactory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Patch, CombineUrl(_baseUrl, relativeUrl));
        AddAuthorizationHeader(request);

        request.Content = content;

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
        var httpClient = _httpClientFactory.CreateClient();
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
        var httpClient = _httpClientFactory.CreateClient();
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

    private void AddAuthorizationHeader(HttpRequestMessage request)
    {
        request.Headers.Add("api-key", $"{_apiKey}");
        request.Headers.Add("email", $"{_email}");
    }

    private static string CombineUrl(string baseUrl, string relativeUrl)
    {
        // Ensure the base URL ends with a slash
        if (!baseUrl.EndsWith("/"))
        {
            baseUrl += "/";
        }

        // Ensure the relative URL does not start with a slash
        if (relativeUrl.StartsWith("/"))
        {
            relativeUrl = relativeUrl.TrimStart('/');
        }

        return baseUrl + relativeUrl;
    }
}
