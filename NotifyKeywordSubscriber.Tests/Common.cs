using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NotifyKeywordSubscriber.Tests
{
    [TestClass]
    public class Common
    {
        private string[] Urls { get; set; }

        private static Random GetRandom()
        {
            return new Random(DateTime.Now.Millisecond);
        }

        [TestInitialize]
        public void Setup()
        {
            this.Urls = new string[] {
                "http://feeds.reuters.com/Reuters/worldNews",
                "http://newsrss.bbc.co.uk/rss/newsonline_uk_edition/world/rss.xml",
                "http://rss.cnn.com/rss/edition_world.rss"
            };
        }

        [TestMethod]
        public void FeedsShouldContainItems()
        {
            var feeds = Api.GetFeeds(this.Urls).ToArray();

            try
            {
                foreach (var feed in feeds)
                {
                    Console.WriteLine("==============================================");
                    Console.WriteLine($"{DateTime.Now}: Downloaded {feed.Title} feed:");
                    foreach (var item in feed.Items)
                    {
                        Console.WriteLine($"{item.Title}");
                        Console.WriteLine($"{item.Link}");
                        if (!String.IsNullOrEmpty(item.Description))
                        {
                            Console.WriteLine($"{item.Description}");
                        }
                        Console.WriteLine("--------------");
                    }
                }

                Assert.AreEqual(this.Urls.Length, feeds.Count());
                foreach (var feed in feeds)
                {
                    Assert.IsTrue(feed.Items.Any());
                }
            }
            catch (Exception ex) {
                Assert.Fail($"Test failed as exception thrown while iterating feeds: {ex}.");
            }
        }

        [TestMethod]
        public void UsersShouldBeAdded()
        {
            var sourceList = new string[] { "John", "Billy", "Emma", "Deborah" }.ToList();

            sourceList.ForEach(name => Api.AddUser(name));

            var users = Api.GetUsers();
            Assert.AreEqual(sourceList.Count, users.Count());
        }

        [TestMethod]
        [ExpectedException(typeof(Exceptions.NksException))]
        public void UserDuplicateShouldBeRejected()
        {
            var sourceList = new string[] { "Alice", "Berenice", "Cynthia", "David" }.ToList();

            sourceList.ForEach(name => Api.AddUser(name));

            Api.AddUser(sourceList.First());
        }

        private void AllocUserKeywords(IEnumerable<Models.User> users)
        {
            var rnd = GetRandom();

            // news buzzwords on nowdays. Feel free to change as you wish
            var keywords = new string[] { "Elections", "Panama", "ISIS", "Syria", "Putin", "Russia", "Iran", "Ukraine" };

            // random keyword picker for a user
            var getRandomKeyword = new Func<string>(() => keywords[rnd.Next(keywords.Length)]);

            // keywords are to be randomly distributed to the available users (there may also be present users from other tests)
            users.AsParallel().ForAll(user => {
                var userKeywords = new string[rnd.Next(4) + 1] // a user will contain from 1 to 4 keywords
                    .Select(_ => getRandomKeyword())
                    .ToList();

                // loop performed while there are duplicate keywords in it. Just to meet preset userKeywords.Count
                while (userKeywords.Distinct().Count() < userKeywords.Count)
                {
                    userKeywords = userKeywords
                        .Distinct()
                        .Union(new[] { getRandomKeyword() })
                        .ToList();
                }

                user.AddUserKeywords(userKeywords.ToArray());
            });

            // display results of random keyword assignment to a user
            foreach (var user in users)
            {
                Console.WriteLine($"{user.Name} is watching for news about:");
                foreach (var kw in user.Keywords)
                {
                    Console.WriteLine($"\t{kw}");
                }
            }
        }

        [TestMethod]
        public void FeedUsersDistributionShowcase()
        {
            // create 4 users
            var users = new string[] { "Kyle", "Kenny", "Eric", "Stan" }
                .Select(name => Api.AddUser(name))
                .ToArray();

            // assign random keywords for them
            AllocUserKeywords(users);

            // now start the target distribution showing only those feeds that the local users are interested in
            var results = Api.GetFeedUsers(users, Api.GetFeeds(this.Urls));
            foreach (var result in results)
            {
                var item = result.Item1;
                var itemUsers = result.Item2.ToArray();

                Console.WriteLine("========");
                Console.WriteLine(item.Title);
                Console.WriteLine(item.Link);
                Console.WriteLine($">>> {itemUsers.Length} users are interested:");
                foreach (var user in itemUsers)
                {
                    Console.WriteLine($"\tnotified {user.Name} (interested in {String.Join(", ", user.Keywords)})");
                }
            }
        }

        [TestMethod]
        public void UserNotificationShowcase()
        {
            // create 4 users
            var users = new string[] { "Vinnie-the-Pooh", "Piglet", "Rabbit", "Tigger" }
                .Select(name => Api.AddUser(name))
                .ToArray();

            // assign random keywords for them
            AllocUserKeywords(users);

            // materialise feeds
            var feeds = Api.GetFeeds(this.Urls).ToArray();

            // invoke the notification process,
            // so that the users are notified of the news interesting to them
            Api.NotifyUsers(
                users,
                feeds,
                (user, matches) => {
                    var items = matches.ToArray();
                    Console.WriteLine("========");
                    Console.WriteLine($"User {user.Name} (interested in {String.Join(", ", user.Keywords)}) notified with the following {items.Length} update{(items.Length > 1 ? "s" : String.Empty)}.");
                    foreach (var item in items)
                    {
                        Console.WriteLine("--------");
                        Console.WriteLine($"\t{item.Title}");
                        Console.WriteLine($"\t{item.Link}");
                    }
                }
            );
        }
    }
}
