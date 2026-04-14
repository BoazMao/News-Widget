using System;
using System.Collections.Generic;

namespace sellthenews.Models
{
    public class SellTheNewsLiveItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string BodyHtml { get; set; }
        public DateTime Time { get; set; }
        public string Url { get; set; }
        public string Source { get; set; }
        public string SourceLabel { get; set; }
        public int Coverage { get; set; }
        public List<TickerInfo> Tickers { get; set; }

        public SellTheNewsLiveItem()
        {
            Id = "";
            Title = "";
            BodyHtml = "";
            Time = DateTime.Now;
            Url = "";
            Source = "";
            SourceLabel = "";
            Coverage = 0;
            Tickers = new List<TickerInfo>();
        }
    }

    public class TickerInfo
    {
        public string Symbol { get; set; }
        public string Direction { get; set; }
    }
}
