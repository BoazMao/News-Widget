# News Widget

A focused Windows desktop workspace for current headlines and WSB market analysis.

## Workspaces

- **News** — US top headlines from [NewsAPI](https://newsapi.org/) in five native categories: General, Business, Technology, Science, and Health.
- **WSB** — the latest Sell The News WSB analysis report.

The former Sell The News live pipeline is disabled and retained only as source reference while NewsAPI replaces it.

## NewsAPI setup

1. Create a NewsAPI account and copy your API key.
2. Launch News Widget and choose **Settings**.
3. Paste the key and select **Save key**.

The key is never stored in this repository or placed in request URLs. It is encrypted with Windows Data Protection for the current Windows user and sent only in the `X-Api-Key` request header. Clearing the field and saving removes the local key.

## Behavior

- News refreshes every 10 minutes and on demand.
- WSB refreshes hourly and on demand.
- Refreshes do not overlap.
- NewsAPI authentication, rate-limit, timeout, provider, and malformed-response errors are shown without discarding the existing visible headlines.
- Article URLs are deduplicated, removed records are ignored, and timestamps are converted for local display.

## Development

Requires the .NET 10 SDK on Windows.

```powershell
dotnet build sellthenews_sol.slnx
dotnet run --project sellthenews/sellthenews.csproj
```

No API key is required to compile the application.
