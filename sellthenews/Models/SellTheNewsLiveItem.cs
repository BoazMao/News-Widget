using System;

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