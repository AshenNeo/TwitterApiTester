using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace TwitterApiTester.Twitter
{
    public static class SortedDictionaryHelper
    {
        public static void AddUrlEncodedItem(this SortedDictionary<string, string> d, string key, string value)
        {
            d.Add(key, HttpUtility.UrlEncode(value));
        }
    }
}
