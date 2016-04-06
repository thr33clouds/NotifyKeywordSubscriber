using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotifyKeywordSubscriber.Exceptions
{
    public class NksException : ApplicationException
    {
        public NksException() { }
        public NksException(string message) : base(message) { }
        public NksException(string message, Exception innerException) : base(message, innerException) { }
    }
}
