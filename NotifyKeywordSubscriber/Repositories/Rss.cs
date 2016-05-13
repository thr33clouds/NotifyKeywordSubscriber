using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace NotifyKeywordSubscriber.Repositories
{
    class Rss
    {
        private async Task<Stream> GetHttpStreamAsync(string url, params string[] contentTypes)
        {
            using (var httpcli = new HttpClient())
            {
                var re = (await httpcli.GetAsync(url)).EnsureSuccessStatusCode();

                if (re.Content.Headers.ContentType == null || !contentTypes.Contains(re.Content.Headers.ContentType.MediaType))
                {
                    throw new HttpRequestException($"Unexpected response content-type: {re.Content.Headers.ContentType?.MediaType}.");
                }

                return await re.EnsureSuccessStatusCode().Content.ReadAsStreamAsync();
            }
        }

        public IEnumerable<Models.RssFeed> Get(params string[] urls)
        {
            var gatherFeed = new Func<string, Models.RssFeed>(url =>
            {
                var rslt = new Models.RssFeed();

                Task.Run(async () => {
                    var xdoc = XDocument.Load(await this.GetHttpStreamAsync(url, "text/xml", "application/xml"));
                    var channelElement = xdoc.Root.Element("channel");
                    if (channelElement != null)
                    {
                        rslt.Title = channelElement.Element("title")?.Value;
                        rslt.Link = channelElement.Element("link")?.Value;
                        rslt.Items = channelElement.Elements("item")
                            .Select(el => new Models.RssItem
                            {
                                Title = el.Element("title")?.Value,
                                Link = el.Element("link")?.Value,
                                Description = el.Element("description")?.Value
                            })
                            .Where(item => item.IsValid())
                            .ToArray();
                    }
                    else {
                        throw new Exceptions.NksException($"Invalid RSS structure in response from: {url}.");
                    }
                }).Wait();

                return rslt;
            });

            var tasks = urls
                .Select(url => Task.Factory.StartNew(() => gatherFeed(url)))
                .ToList();

            while (tasks.Any())
            {
                var task = tasks[Task.WaitAny(tasks.ToArray())];
                tasks.Remove(task);
                yield return task.Result;
            }
        }
    }
}
