using System;

namespace sellthenews.Models
{
    public class FinancialJuiceHeadline
    {
        public string Title { get; set; }
        public string Summary { get; set; }
        public DateTime PublishedAt { get; set; }
        public string Source { get; set; }
        public string Category { get; set; }

        public FinancialJuiceHeadline()
        {
            Title = "";
            Summary = "";
            PublishedAt = DateTime.Now;
            Source = "";
            Category = "";
        }
    }
}
