using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using sellthenews.Models;

namespace sellthenews.Services;

public sealed class NewsApiService
{
    private static readonly Uri Endpoint = new("https://newsapi.org/v2/top-headlines");
    private const string ApplicationUserAgent = "NewsWidget/1.0 (+https://github.com/BoazMao/News-Widget)";
    private readonly HttpClient client;

    public NewsApiService(HttpClient client)
    {
        this.client = client;
        this.client.Timeout = TimeSpan.FromSeconds(20);
    }

    public async Task<IReadOnlyList<NewsArticle>> GetTopHeadlinesAsync(
        string? apiKey,
        NewsCategory category,
        CancellationToken cancellationToken = default)
    {
        var uri = new UriBuilder(Endpoint)
        {
            Query = $"country=us&category={category.ToApiValue()}&pageSize=100"
        }.Uri;

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.UserAgent.ParseAdd(ApplicationUserAgent);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrWhiteSpace(apiKey))
            request.Headers.Add("X-Api-Key", apiKey.Trim());

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new NewsApiException("NewsAPI timed out. Try again shortly.");
        }
        catch (HttpRequestException ex)
        {
            throw new NewsApiException("NewsAPI could not be reached. Check your connection.", null, ex);
        }

        using (response)
        {
            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw CreateApiException(response.StatusCode, json);

            try
            {
                return ParseArticles(json, category);
            }
            catch (JsonException ex)
            {
                throw new NewsApiException("NewsAPI returned data in an unexpected format.", (int)response.StatusCode, ex);
            }
        }
    }

    private static IReadOnlyList<NewsArticle> ParseArticles(string json, NewsCategory category)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        if (!root.TryGetProperty("status", out var status) || status.GetString() != "ok")
            throw new NewsApiException(ReadErrorMessage(root));

        if (!root.TryGetProperty("articles", out var articles) || articles.ValueKind != JsonValueKind.Array)
            return Array.Empty<NewsArticle>();

        var results = new List<NewsArticle>();
        var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (JsonElement item in articles.EnumerateArray())
        {
            string? title = ReadString(item, "title");
            string? rawUrl = ReadString(item, "url");
            if (string.IsNullOrWhiteSpace(title) || title == "[Removed]" ||
                !Uri.TryCreate(rawUrl, UriKind.Absolute, out var url) || !seenUrls.Add(url.AbsoluteUri))
                continue;

            string source = "Unknown source";
            if (item.TryGetProperty("source", out var sourceElement))
                source = ReadString(sourceElement, "name") ?? source;

            Uri? imageUrl = Uri.TryCreate(ReadString(item, "urlToImage"), UriKind.Absolute, out var parsedImage)
                ? parsedImage : null;
            DateTimeOffset.TryParse(ReadString(item, "publishedAt"), out var published);

            results.Add(new NewsArticle
            {
                SourceName = source,
                Author = ReadString(item, "author"),
                Title = title.Trim(),
                Description = Clean(ReadString(item, "description")),
                Url = url,
                ImageUrl = imageUrl,
                PublishedAt = published,
                ContentPreview = Clean(ReadString(item, "content")),
                Category = category
            });
        }

        return results.OrderByDescending(article => article.PublishedAt).ToArray();
    }

    private static NewsApiException CreateApiException(HttpStatusCode statusCode, string json)
    {
        string providerMessage;
        try
        {
            using var document = JsonDocument.Parse(json);
            providerMessage = ReadErrorMessage(document.RootElement);
        }
        catch (JsonException)
        {
            providerMessage = "NewsAPI returned an error.";
        }

        string message = statusCode switch
        {
            HttpStatusCode.Unauthorized => "NewsAPI rejected the request. Add or update your API key in Settings.",
            HttpStatusCode.TooManyRequests => "NewsAPI's request limit was reached. Cached headlines remain visible.",
            HttpStatusCode.BadRequest => $"NewsAPI rejected the request: {providerMessage}",
            _ when (int)statusCode >= 500 => "NewsAPI is temporarily unavailable. Try again shortly.",
            _ => providerMessage
        };

        return new NewsApiException(message, (int)statusCode);
    }

    private static string ReadErrorMessage(JsonElement root) =>
        ReadString(root, "message") ?? "NewsAPI returned an error.";

    private static string? ReadString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() : null;

    private static string? Clean(string? text) =>
        string.IsNullOrWhiteSpace(text) ? null : WebUtility.HtmlDecode(text).Trim();
}
