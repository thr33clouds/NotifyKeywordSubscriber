using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotifyKeywordSubscriber.Models
{
    public class RssFeed
    {
        internal RssFeed() { }
        public string Title { get; internal set; }
        public string Link { get; internal set; }
        public IEnumerable<RssItem> Items { get; internal set; }
    }
}
