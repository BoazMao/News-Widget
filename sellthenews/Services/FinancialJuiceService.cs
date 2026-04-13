using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using sellthenews.Models;

namespace sellthenews.Services
{
    public class FinancialJuiceService
    {
        private readonly HttpClient client;
        private List<FinancialJuiceHeadline> cachedHeadlines;

        public FinancialJuiceService(HttpClient httpClient = null)
        {
            client = httpClient ?? new HttpClient();
            cachedHeadlines = new List<FinancialJuiceHeadline>();
        }

        public async Task<List<FinancialJuiceHeadline>> FetchLatestHeadlinesAsync()
        {
            try
            {
                // Using NewsAPI as a working test endpoint for financial news
                // Replace with actual Financial Juice API when credentials are available
                // API Key: newsapi.org free tier (no key required for testing)

                string url = "https://newsapi.org/v2/everything?q=market+OR+stocks+OR+finance&sortBy=publishedAt&language=en&pageSize=10";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                // Note: Add your actual API key when switching to production Financial Juice API
                // request.Headers.Add("Authorization", "Bearer YOUR_FINANCIAL_JUICE_API_KEY");

                using var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    cachedHeadlines = new List<FinancialJuiceHeadline>
                    {
                        new FinancialJuiceHeadline
                        {
                            Title = $"API Error: HTTP {(int)response.StatusCode}",
                            Summary = "Unable to fetch headlines. Check API endpoint and credentials.",
                            PublishedAt = DateTime.Now,
                            Source = "Error"
                        }
                    };
                    return cachedHeadlines;
                }

                string json = await response.Content.ReadAsStringAsync();
                cachedHeadlines = ParseHeadlines(json);
            }
            catch (Exception ex)
            {
                cachedHeadlines = new List<FinancialJuiceHeadline>
                {
                    new FinancialJuiceHeadline
                    {
                        Title = "Error loading headlines",
                        Summary = ex.Message,
                        PublishedAt = DateTime.Now,
                        Source = "Error"
                    }
                };
            }

            return cachedHeadlines;
        }

        private List<FinancialJuiceHeadline> ParseHeadlines(string json)
        {
            var headlines = new List<FinancialJuiceHeadline>();

            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                // NewsAPI response structure: articles array under "articles" property
                if (root.TryGetProperty("articles", out JsonElement articlesElement) && articlesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in articlesElement.EnumerateArray())
                    {
                        var headline = new FinancialJuiceHeadline
                        {
                            Title = item.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                            Summary = item.TryGetProperty("description", out var s) ? s.GetString() ?? "" : "",
                            Source = item.TryGetProperty("source", out var src)
                                ? (src.TryGetProperty("name", out var sname) ? sname.GetString() ?? "" : "")
                                : "",
                            PublishedAt = item.TryGetProperty("publishedAt", out var pub)
                                ? DateTime.TryParse(pub.GetString(), out var dt) ? dt : DateTime.Now
                                : DateTime.Now
                        };

                        if (!string.IsNullOrWhiteSpace(headline.Title))
                            headlines.Add(headline);
                    }
                }
                // Fallback: direct array if response structure is different
                else if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in root.EnumerateArray())
                    {
                        var headline = new FinancialJuiceHeadline
                        {
                            Title = item.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                            Summary = item.TryGetProperty("summary", out var s) ? s.GetString() ?? "" : "",
                            Source = item.TryGetProperty("source", out var src) ? src.GetString() ?? "" : "",
                            Category = item.TryGetProperty("category", out var cat) ? cat.GetString() ?? "" : "",
                            PublishedAt = item.TryGetProperty("publishedAt", out var pub)
                                ? DateTime.TryParse(pub.GetString(), out var dt) ? dt : DateTime.Now
                                : DateTime.Now
                        };

                        if (!string.IsNullOrWhiteSpace(headline.Title))
                            headlines.Add(headline);
                    }
                }
            }
            catch
            {
                // Return empty or cached headlines on parse error
            }

            return headlines.Any() ? headlines : cachedHeadlines;
        }

        public List<FinancialJuiceHeadline> GetCachedHeadlines()
        {
            return cachedHeadlines;
        }
    }
}
