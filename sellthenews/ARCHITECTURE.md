## Sell The News Lite – Multi-Feed Desktop Widget

### Architecture Overview

The app has evolved from a single-feed widget into a lightweight multi-feed market desktop app while maintaining the current WinForms architecture and low-resource design.

---

### File Structure

```
sellthenews/
├── Models/
│   ├── SellTheNewsSummary.cs       # Data model for Sell The News feed
│   └── FinancialJuiceHeadline.cs   # Data model for Financial Juice feed
├── Services/
│   ├── SellTheNewsService.cs       # Fetch and parse Sell The News endpoint
│   └── FinancialJuiceService.cs    # Fetch and parse Financial Juice endpoint
└── Form1.cs                         # Main UI with tab navigation
```

---

### Key Features

#### 1. **Multi-Feed Navigation**
- **Overview**: Dashboard combining both feeds (Sell The News snippet + Financial Juice headline list)
- **Sell The News**: Full-width Sell The News content
- **Financial Juice**: Scrollable list of headlines with timestamps and categories

#### 2. **Modular Services**
- `SellTheNewsService`: Handles fetching and parsing Sell The News API
  - `FetchLatestSummaryAsync()`: Fetches the latest summary
  - `ExtractMainSection()`: Extracts the main section (## 1. to ## 2.)
  
- `FinancialJuiceService`: Handles fetching and parsing Financial Juice API
  - `FetchLatestHeadlinesAsync()`: Fetches recent headlines
  - `GetCachedHeadlines()`: Returns cached data if fetch fails
  - Caching layer built-in to show last successful load on refresh failure

#### 3. **Independent Refresh Timers**
- Sell The News: 30-second interval
- Financial Juice: 45-second interval
- Both run asynchronously without blocking UI
- Initial load triggers on form startup

#### 4. **Error Handling**
- If one feed fails, the other continues to work
- Failed refreshes show error message but preserve last successful data
- No full-UI replacement with exception dumps

#### 5. **Widget Design**
- Compact, always-on-top window (500×380 pixels)
- Positioned top-right of screen on startup
- Draggable by title labels and tab area
- Rounded corners (12px radius)
- Subtle transparency (0.92 opacity)
- Dark theme matching current design
- Borderless with no console window

---

### UI Layout

#### Tab Navigation (Height: 36px)
- "Overview", "Sell The News", "Financial Juice" buttons
- Close (×) button top-right
- Dark background with hover/selected highlighting

#### Content Panels (Height: 344px)
Each panel contains:
- **Overview**: Title label + Updated time + Shortened content excerpt
- **Sell The News**: Title label + Updated time + Full extracted section (scrollable)
- **Financial Juice**: ListBox with headlines formatted as:
  ```
  [HH:mm] Headline Title (Category)
    → Shortened summary
  
  [HH:mm] Next Headline (Category)
    → Shortened summary
  ```

---

### Adding a New Feed

To add a third feed source (e.g., MarketWatch):

1. **Create a model** in `Models/`:
   ```csharp
   public class MarketWatchHeadline
   {
       public string Title { get; set; }
       public string Link { get; set; }
       public DateTime PublishedAt { get; set; }
   }
   ```

2. **Create a service** in `Services/`:
   ```csharp
   public class MarketWatchService
   {
       public async Task<List<MarketWatchHeadline>> FetchHeadlinesAsync() { ... }
   }
   ```

3. **Add UI in Form1.cs**:
   - Create a new tab button in `SetupTabs()`
   - Create a new panel in `SetupPanels()` (e.g., `marketWatchPanel`)
   - Add a refresh method `RefreshMarketWatch()`
   - Add a timer in `SetupTimers()`
   - Update `ShowTab()` to handle the new index

---

### Configuration & Extension Points

#### Financial Juice API Integration
The Financial Juice service includes TODO markers for:
- **Endpoint URL**: Replace `https://api.financialjuice.com/v1/headlines?limit=15`
- **Authentication**: Add headers if API requires auth
- **Response parsing**: Update JSON parsing if endpoint structure differs

Current placeholder implementation accepts any JSON array of objects with:
- `title`, `summary`, `source`, `category`, `publishedAt` fields

#### Customization Options
- **Refresh intervals**: Modify `Interval` values in `SetupTimers()`
- **Window size**: Change `Size = new Size(500, 380)` in `SetupWindow()`
- **Colors**: Update `Color.FromArgb()` values throughout UI setup
- **Font sizes**: Modify font sizes in label/textbox initialization
- **Content truncation**: Adjust `maxLength` parameters in `ShortenContent()`

---

### Performance & Design Decisions

✅ **Lightweight**: No external UI frameworks, pure WinForms
✅ **Non-blocking**: Async/await for all network calls
✅ **Efficient**: Separate timers prevent one slow feed blocking another
✅ **Resilient**: Caching layer in Financial Juice service
✅ **Extensible**: Simple service pattern for adding feeds
✅ **Low memory**: No persistent browser or heavy controls

---

### Testing the App

1. Build the solution
2. Run `sellthenews.exe`
3. Widget appears top-right of desktop
4. Click tabs to navigate between feeds
5. Drag by title area to move window
6. Press ESC or click × to close

Initial load populates Overview tab with Sell The News data. Financial Juice will show placeholder until endpoint is configured.

---

### Next Steps

1. **Get Financial Juice API credentials** and update the endpoint URL and authentication in `FinancialJuiceService.cs`
2. **Test parsing** against actual Financial Juice response format
3. **Adjust refresh intervals** based on API rate limits
4. **Add more feeds** following the same pattern
5. **Refine UI layout** if needed (font sizes, spacing, colors)
