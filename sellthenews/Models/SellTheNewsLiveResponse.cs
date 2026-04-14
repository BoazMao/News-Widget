using System;
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