using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using sellthenews.Models;

namespace sellthenews.Services
{
    public class SellTheNewsLiveService
    {
        private readonly HttpClient client;
        private SellTheNewsLiveResponse cachedResponse;

        public SellTheNewsLiveService(HttpClient httpClient = null)
        {
            client = httpClient ?? new HttpClient();
            cachedResponse = new SellTheNewsLiveResponse();
        }

        public async Task<SellTheNewsLiveResponse> FetchLiveNewsAsync()
        {
            try
            {
                string url =
                    $"https://sellthenews.org/api/live/recent?limit=15&offset=0&lang=zh&sources=wsj,nyt,bloomberg,ft&marketOnly=1&_ts={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Pragma.ParseAdd("no-cache");
                request.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
                {
                    NoCache = true,
                    NoStore = true,
                    MustRevalidate = true,
                    MaxAge = TimeSpan.Zero
                };

                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                var liveResponse = ParseLiveResponse(json);
                liveResponse.FetchedAt = DateTime.Now;

                cachedResponse = liveResponse;
                return liveResponse;
            }
            catch
            {
                return cachedResponse;
            }
        }

        private SellTheNewsLiveResponse ParseLiveResponse(string json)
        {
            var response = new SellTheNewsLiveResponse();

            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;

            if (root.TryGetProperty("pinnedPosts", out JsonElement pinnedElement) &&
                pinnedElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in pinnedElement.EnumerateArray())
                {
                    var parsed = ParseLiveItem(item);
                    if (parsed != null)
                    {
                        response.PinnedPosts.Add(parsed);
                    }
                }
            }

            if (root.TryGetProperty("data", out JsonElement dataElement) &&
                dataElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in dataElement.EnumerateArray())
                {
                    var parsed = ParseLiveItem(item);
                    if (parsed != null)
                    {
                        response.Data.Add(parsed);
                    }
                }
            }

            return response;
        }

        private SellTheNewsLiveItem ParseLiveItem(JsonElement element)
        {
            try
            {
                var item = new SellTheNewsLiveItem
                {
                    Title = element.TryGetProperty("title", out var title)
                        ? title.GetString() ?? ""
                        : "",
                    Body = element.TryGetProperty("bodyHtml", out var bodyHtml)
                        ? HtmlToPlainText(bodyHtml.GetString() ?? "")
                        : "",
                    Source = GetSourceText(element),
                    Time = ParseTime(element)
                };

                return item;
            }
            catch
            {
                return null;
            }
        }

        private string GetSourceText(JsonElement element)
        {
            if (element.TryGetProperty("sourceLabel", out var sourceLabel))
            {
                string label = sourceLabel.GetString() ?? "";
                if (!string.IsNullOrWhiteSpace(label))
                    return label;
            }

            if (element.TryGetProperty("source", out var source))
            {
                return source.GetString() ?? "";
            }

            return "";
        }

        private DateTime ParseTime(JsonElement element)
        {
            if (element.TryGetProperty("time", out var timeElement))
            {
                string timeText = timeElement.GetString() ?? "";
                if (DateTime.TryParse(timeText, out var parsed))
                    return parsed;
            }

            return DateTime.Now;
        }

        private string HtmlToPlainText(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return "";

            string text = html;

            text = Regex.Replace(text, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"</p\s*>", "\n\n", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"<li\s*>", "• ", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"</li\s*>", "\n", RegexOptions.IgnoreCase);

            text = Regex.Replace(text, @"<[^>]+>", "");
            text = System.Net.WebUtility.HtmlDecode(text);

            text = Regex.Replace(text, @"\n{3,}", "\n\n");
            return text.Trim();
        }

        public SellTheNewsLiveResponse GetCachedResponse()
        {
            return cachedResponse;
        }
    }
}