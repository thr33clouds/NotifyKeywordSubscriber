using System;

namespace NotifyKeywordSubscriber.Models
{
    public class RssItem
    {
        internal RssItem() { }
        public string Title { get; internal set; }
        public string Link { get; internal set; }
        public string Description { get; internal set; }

        internal bool IsValid()
        {
            return !(String.IsNullOrEmpty(this.Title) || String.IsNullOrEmpty(this.Link));
        }
    }
}