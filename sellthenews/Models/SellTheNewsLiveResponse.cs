// RETIRED: The Sell The News live pipeline is intentionally disabled. NewsAPI now owns the News workspace.\n// This file is retained temporarily as reference and is not constructed, timed, refreshed, or displayed.\n\nusing System;
using System.Collections.Generic;

namespace sellthenews.Models
{
    public class SellTheNewsLiveResponse
    {
        public List<SellTheNewsLiveItem> Data { get; set; }
        public List<SellTheNewsLiveItem> PinnedPosts { get; set; }
        public DateTime FetchedAt { get; set; }

        public SellTheNewsLiveResponse()
        {
            Data = new List<SellTheNewsLiveItem>();
            PinnedPosts = new List<SellTheNewsLiveItem>();
            FetchedAt = DateTime.Now;
        }
    }
}