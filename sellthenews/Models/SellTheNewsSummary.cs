using System;

namespace sellthenews.Models
{
    public class SellTheNewsSummary
    {
        public string Title { get; set; }
        public string Markdown { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Section { get; set; }

        public SellTheNewsSummary()
        {
            Title = "No title";
            Markdown = "";
            UpdatedAt = DateTime.Now;
            Section = "";
        }
    }
}
