using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NotifyKeywordSubscriber.Models;

namespace NotifyKeywordSubscriber
{
    public static class Api
    {
        const string TITLE_DIVIDERS = @" ,./\'!&*()-=+;:[]{}<>?";

        public static IEnumerable<Models.RssFeed> GetFeeds(params string[] urls)
        {
            return new Repositories.Rss().Get(urls);
        }

        public static IEnumerable<Models.User> GetUsers()
        {
            return new Repositories.Users().GetAll();
        }

        public static Models.User AddUser(string name) {
            var repo = new Repositories.Users();
            if (repo.Exists(name)) {
                throw new Exceptions.NksException($"{name} is already registered.");
            }

            var user = new Models.User() { Name = name };
            repo.Save(user);
            return user;
        }

        public static void AddUserKeywords(this Models.User user, params string[] keywords)
        {
            var list = user.Keywords.ToList();

            foreach (var word in keywords.Where(w => !String.IsNullOrWhiteSpace(w)))
            {
                if (!user.UserWatchingKeyword(word))
                {
                    list.Add(word);
                }
            }

            user.Keywords = list.ToArray();
            user.KeywordDictionary = user.Keywords.ToDictionary(w => w.ToLower());
            new Repositories.Users().Save(user);
        }

        internal static bool UserWatchingKeyword(this Models.User user, string keyword) {
            if (user.KeywordDictionary == null)
            {
                return user.Keywords.Count(w => w.Equals(keyword, StringComparison.CurrentCultureIgnoreCase)) > 0;
            }
            else {
                return user.KeywordDictionary.ContainsKey(keyword.ToLower());
            }
        }

        /// <summary>
        /// Returns rss-items along with the users who might be interested.
        /// </summary>
        public static IEnumerable<Tuple<RssItem, IEnumerable<Models.User>>> GetFeedUsers(IEnumerable<User> users, IEnumerable<RssFeed> feeds)
        {

            // pivot the keywords to have keyword-to-users dictionary
            var uptask = Task.Factory.StartNew(() => users
                    .SelectMany(user => user.Keywords)
                    .Distinct()
                    .ToDictionary(kw => kw.ToLower(), kw => users.Where(u => u.UserWatchingKeyword(kw))
                    .ToArray())
            );

            // prepare matching users selector
            var getTitleMatches = new Func<string, IEnumerable<Models.User>>(title => {
                var words = title.Split(TITLE_DIVIDERS.ToArray()).Distinct();
                return words
                    .Select(word => word.ToLower())
                    .Where(word => uptask.Result.ContainsKey(word))
                    .SelectMany(word => uptask.Result[word])
                    .Distinct();
            });


            // iterate through feeds joining matching users
            return feeds
                .SelectMany(feed => feed.Items)
                .Select(item => Tuple.Create(item, getTitleMatches(item.Title)))
                .Where(tuple => tuple.Item2.Any());
        }

        private static void NotifyUser(this User user, IEnumerable<RssItem> matches, Action<Models.User, IEnumerable<RssItem>> onNotified)
        {
            // there is nowhere to send really as this is a prototype,
            // so just invoke the callback
            onNotified(user, matches);
        }

        /// <summary>
        /// Notifies those users who have an interesting news for them.
        /// </summary>
        public static void NotifyUsers(IEnumerable<User> users, IEnumerable<RssFeed> feeds, Action<Models.User, IEnumerable<RssItem>> onNotified)
        {
            // prepare matching items selector
            var getUserMatches = new Func<User, IDictionary<RssItem, string[]>, IEnumerable<RssItem>>((user, items) => {
                return items
                    .Where(kvp => kvp.Value.Any(word => user.UserWatchingKeyword(word)))
                    .Select(kvp => kvp.Key);
            });

            // pre-combine feeds to save cycles on per-user basis
            var feedItemDic = feeds
                .SelectMany(feed => feed.Items)
                .ToDictionary(item => item, item => item.Title.Split(TITLE_DIVIDERS.ToArray()));

            // combine users with matching feed-items
            users
                .Select(user => new { User = user, Items = getUserMatches(user, feedItemDic) })
                .Where(x => x.Items.Any())
                .ToList()
                .ForEach(x => x.User.NotifyUser(x.Items, onNotified));
        }
    }
}
