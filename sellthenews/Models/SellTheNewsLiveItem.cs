// RETIRED: The Sell The News live pipeline is intentionally disabled. NewsAPI now owns the News workspace.
// This file is retained temporarily as reference and is not constructed, timed, refreshed, or displayed.

// RETIRED: The Sell The News live pipeline is intentionally disabled. NewsAPI now owns the News workspace.\n// This file is retained temporarily as reference and is not constructed, timed, refreshed, or displayed.\n\nusing System;

namespace sellthenews.Models
{
    public class SellTheNewsLiveItem
    {
        public string Title { get; set; }
        public string Body { get; set; }
        public string Source { get; set; }
        public DateTime Time { get; set; }

        public SellTheNewsLiveItem()
        {
            Title = "";
            Body = "";
            Source = "";
            Time = DateTime.Now;
        }
    }
}