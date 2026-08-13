namespace sellthenews.Models;

public sealed class NewsArticle
{
    public string SourceName { get; init; } = "Unknown source";
    public string? Author { get; init; }
    public string Title { get; init; } = "Untitled";
    public string? Description { get; init; }
    public Uri Url { get; init; } = new("https://newsapi.org");
    public Uri? ImageUrl { get; init; }
    public DateTimeOffset PublishedAt { get; init; }
    public string? ContentPreview { get; init; }
    public NewsCategory Category { get; init; }

    public string DisplayTime =>
        PublishedAt == default ? "Time unavailable" : PublishedAt.ToLocalTime().ToString("MMM d, h:mm tt");
}
