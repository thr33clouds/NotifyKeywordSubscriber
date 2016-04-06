using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotifyKeywordSubscriber.Repositories
{
    class Users
    {
        private static readonly ConcurrentDictionary<string, Models.User> storage = new ConcurrentDictionary<string, Models.User>();

        public IEnumerable<Models.User> GetAll()
        {
            return storage
                .Select(kvp => kvp.Value)
                .ToArray();
        }

        public bool Exists(string name)
        {
            return storage.ContainsKey(name);
        }

        public void Save(Models.User user)
        {
            storage.AddOrUpdate(user.Name, user, (key, u) => user);
        }

        public void Delete(string name)
        {
            Models.User user;
            storage.TryRemove(name, out user);
        }
    }
}
