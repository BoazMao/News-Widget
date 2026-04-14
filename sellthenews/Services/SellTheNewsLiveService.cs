using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using sellthenews.Models;

namespace sellthenews.Services
{
    public class SellTheNewsLiveService
    {
        private readonly HttpClient client;
        private string lastEtag = "";
        private SellTheNewsLiveResponse cachedResponse;

        public SellTheNewsLiveService(HttpClient httpClient = null)
        {
            client = httpClient ?? new HttpClient();
            cachedResponse = new SellTheNewsLiveResponse();
        }

        /// <summary>
        /// Fetches live news with ETag-based caching.
        /// Returns null if server returns 304 Not Modified.
        /// Returns the response if new data available (200 OK).
        /// </summary>
        public async Task<SellTheNewsLiveResponse> FetchLiveNewsAsync()
        {
            try
            {
                string url = "https://sellthenews.org/api/live/recent?limit=15&offset=0&lang=zh&sources=wsj,nyt,bloomberg,ft&marketOnly=1";

                var request = new HttpRequestMessage(HttpMethod.Get, url);

                // Add If-None-Match header if we have a cached ETag
                if (!string.IsNullOrWhiteSpace(lastEtag))
                {
                    request.Headers.Add("If-None-Match", lastEtag);
                    System.Diagnostics.Debug.WriteLine($"FetchLiveNewsAsync: Sending If-None-Match: {lastEtag}");
                }

                var response = await client.SendAsync(request);

                // 304 Not Modified - use cached data
                if ((int)response.StatusCode == 304)
                {
                    System.Diagnostics.Debug.WriteLine("FetchLiveNewsAsync: Got 304 Not Modified");
                    return null; // Signal that no update is needed
                }

                // 200 OK - parse new data
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"FetchLiveNewsAsync: Got 200 OK, JSON length: {json.Length}");
                    System.Diagnostics.Debug.WriteLine($"FetchLiveNewsAsync: JSON first 500 chars: {json.Substring(0, Math.Min(500, json.Length))}");

                    var liveResponse = ParseLiveResponse(json);

                    // Store the new ETag for next request
                    if (response.Headers.ETag != null)
                    {
                        lastEtag = response.Headers.ETag.Tag;
                        System.Diagnostics.Debug.WriteLine($"FetchLiveNewsAsync: Stored ETag: {lastEtag}");
                    }

                    liveResponse.FetchedAt = DateTime.Now;
                    cachedResponse = liveResponse;

                    System.Diagnostics.Debug.WriteLine($"FetchLiveNewsAsync: Parsed {liveResponse.Data.Count} items, {liveResponse.PinnedPosts.Count} pinned");

                    return liveResponse;
                }

                System.Diagnostics.Debug.WriteLine($"FetchLiveNewsAsync: Got status {response.StatusCode}");
                // Other error
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching live news: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        private SellTheNewsLiveResponse ParseLiveResponse(string json)
        {
            var response = new SellTheNewsLiveResponse();

            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                System.Diagnostics.Debug.WriteLine($"ParseLiveResponse: Root element kind = {root.ValueKind}");

                // Extract ETag if present in response body
                if (root.TryGetProperty("etag", out JsonElement etagElement))
                {
                    response.Etag = etagElement.GetString() ?? "";
                }

                // Parse pinned posts
                if (root.TryGetProperty("pinnedPosts", out JsonElement pinnedElement) && pinnedElement.ValueKind == JsonValueKind.Array)
                {
                    System.Diagnostics.Debug.WriteLine($"ParseLiveResponse: Found pinnedPosts array");
                    foreach (var item in pinnedElement.EnumerateArray())
                    {
                        var liveItem = ParseLiveItem(item);
                        if (liveItem != null)
                        {
                            response.PinnedPosts.Add(liveItem);
                            System.Diagnostics.Debug.WriteLine($"  - Added pinned post: {liveItem.Title}");
                        }
                    }
                }

                // Parse regular data items
                if (root.TryGetProperty("data", out JsonElement dataElement) && dataElement.ValueKind == JsonValueKind.Array)
                {
                    System.Diagnostics.Debug.WriteLine($"ParseLiveResponse: Found data array with {dataElement.GetArrayLength()} items");
                    foreach (var item in dataElement.EnumerateArray())
                    {
                        var liveItem = ParseLiveItem(item);
                        if (liveItem != null)
                        {
                            response.Data.Add(liveItem);
                            System.Diagnostics.Debug.WriteLine($"  - Added item: {liveItem.Title}");
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"ParseLiveResponse: No 'data' property found or not array");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing live response: {ex.Message}\n{ex.StackTrace}");
            }

            return response;
        }

        private SellTheNewsLiveItem ParseLiveItem(JsonElement element)
        {
            try
            {
                var item = new SellTheNewsLiveItem();

                item.Id = element.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";
                item.Title = element.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "";
                item.BodyHtml = element.TryGetProperty("bodyHtml", out var body) ? body.GetString() ?? "" : "";
                item.Url = element.TryGetProperty("url", out var url) ? url.GetString() ?? "" : "";
                item.Source = element.TryGetProperty("source", out var source) ? source.GetString() ?? "" : "";

                // SourceLabel comes from "source" field (e.g., "wsj", "ft", "nyt", "bloomberg")
                // Or fall back to "coverage" if source is empty
                if (element.TryGetProperty("source", out var srcElement))
                {
                    item.SourceLabel = srcElement.GetString() ?? "";
                }
                else if (element.TryGetProperty("coverage", out var coverageStrElement))
                {
                    item.SourceLabel = coverageStrElement.GetString() ?? "";
                }

                // Parse time
                if (element.TryGetProperty("time", out var timeElement))
                {
                    string timeStr = timeElement.GetString() ?? "";
                    if (DateTime.TryParse(timeStr, out var parsedTime))
                        item.Time = parsedTime;
                }

                // Parse tickers array (if it's an array, otherwise skip)
                if (element.TryGetProperty("tickers", out var tickersElement) && tickersElement.ValueKind == JsonValueKind.Array)
                {
                    var tickerList = new List<TickerInfo>();
                    foreach (var ticker in tickersElement.EnumerateArray())
                    {
                        try
                        {
                            var tickerInfo = new TickerInfo();
                            if (ticker.TryGetProperty("symbol", out var symbol))
                                tickerInfo.Symbol = symbol.GetString() ?? "";
                            if (ticker.TryGetProperty("direction", out var direction))
                                tickerInfo.Direction = direction.GetString() ?? "";

                            if (!string.IsNullOrWhiteSpace(tickerInfo.Symbol))
                                tickerList.Add(tickerInfo);
                        }
                        catch { }
                    }
                    item.Tickers = tickerList;
                }

                return item;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the last cached response without making a network request.
        /// </summary>
        public SellTheNewsLiveResponse GetCachedResponse()
        {
            return cachedResponse;
        }
    }
}
