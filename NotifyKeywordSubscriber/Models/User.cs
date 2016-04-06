using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotifyKeywordSubscriber.Models
{
    public class User
    {
        internal User() {
            this.Keywords = new string[0];
        }
        public string Name { get; internal set; }
        public IEnumerable<string> Keywords { get; internal set; }
        internal Dictionary<string, string> KeywordDictionary { get; set; }

        internal object ToList()
        {
            throw new NotImplementedException();
        }
    }
}
