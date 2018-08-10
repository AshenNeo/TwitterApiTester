using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TwitterApiTester.Twitter
{
    public class TwitterApi
    {
        private const string API_ROOT = @"https://api.twitter.com/";

        public static string GetBearerToken => $"{API_ROOT}oauth2/token";

        public static string GetAccessToken => $"{API_ROOT}oauth/access_token";

        public static string GetRequestToken => $"{API_ROOT}oauth/request_token";

        public static string GetUserTimeline => $"{API_ROOT}1.1/statuses/user_timeline.json";

    }
}
