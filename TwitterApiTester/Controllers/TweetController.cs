using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;
using TwitterApiTester.Twitter;

namespace TwitterApiTester.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class TweetController : ControllerBase
    {
        private const string CALLBACK_URL = "http://127.0.0.1/TwitterApiTester/api/Tweet/ExecTweet";
        private const string QUERY_PARAM_OAUTH_TOKEN = "oauth_token";
        private const string QUERY_PARAM_OAUTH_VERIFIER = "oauth_verifier";
        private const string QUERY_PARAM_AUTHORIZATION_ID = "authorization_id";
        private const long USER_ID = 133684052;   // サントリー公式のUserID

        private const string SESSION_USER_ACCESS_TOKEN = "TwitterUserAccessToken";
        private const string SESSION_USER_ACCESS_TOKEN_SECRET = "TwitterUserAccessTokenSecret";

        private readonly TwitterApiToken _twitterApiToken;

        public class TweetResult
        {
            public bool Result { get; set; }
            public string AuthorizationUri { get; set; }

        }


        public TweetController(IOptions<TwitterApiToken> optionsAccessor)
        {
            _twitterApiToken = optionsAccessor.Value;
        }

        [HttpGet]
        public TweetResult ExecTweet(string oauth_token, string denied)
        {
            var successTweet = false;
            var result = new TweetResult();

            // ユーザーアクセストークンが保存されている場合はそのまま使う
            // 認証切れ等でトークンが無効な場合、Tweet実行結果が NULL となる。
            var userAccessToken = HttpContext.Session.GetString(SESSION_USER_ACCESS_TOKEN);
            var userAccessTokenSecret = HttpContext.Session.GetString(SESSION_USER_ACCESS_TOKEN_SECRET);
            if (!string.IsNullOrEmpty(userAccessToken) && !string.IsNullOrEmpty(userAccessTokenSecret))
            {
                var userCreds = Auth.SetUserCredentials(_twitterApiToken.ConsumerApiKey,
                    _twitterApiToken.ConsumerApiSecretKey, userAccessToken, userAccessTokenSecret);
                successTweet = TweetCore(userCreds);
            }

            // Tweet成功
            if (successTweet)
            {
                result.Result = true;
                return result;
            }

            // ユーザーが認証をキャンセルした場合
            if (denied != null)
            {
                this.HttpContext.Response.Redirect("https://google.com");
                result.Result = true;
                return null;
            }

            var appCredentials = new TwitterCredentials(_twitterApiToken.ConsumerApiKey, _twitterApiToken.ConsumerApiSecretKey);
            Auth.SetCredentials(appCredentials);

            if (Request.Query.ContainsKey(QUERY_PARAM_OAUTH_TOKEN) && Request.Query.ContainsKey(QUERY_PARAM_OAUTH_VERIFIER) && Request.Query.ContainsKey(QUERY_PARAM_AUTHORIZATION_ID))
            {
                // Twitter連携の結果で実行された場合
                var requestToken = Request.Query[QUERY_PARAM_OAUTH_TOKEN];
                var requestTokenVerifier = Request.Query[QUERY_PARAM_OAUTH_VERIFIER];
                var authorizationId = Request.Query[QUERY_PARAM_AUTHORIZATION_ID];

                // ユーザーのアクセストークンとシークレットを保存してTweet実行
                var userCreds = AuthFlow.CreateCredentialsFromVerifierCode(requestTokenVerifier, authorizationId);
                HttpContext.Session.SetString(SESSION_USER_ACCESS_TOKEN, userCreds.AccessToken);
                HttpContext.Session.SetString(SESSION_USER_ACCESS_TOKEN_SECRET, userCreds.AccessTokenSecret);
                successTweet = TweetCore(userCreds);

                // 結果ページにリダイレクト
                if (successTweet)
                {
                    this.HttpContext.Response.Redirect("https://twitter.com/");
                    result.Result = true;
                    return null;
                }
            }

            var authenticationContext = AuthFlow.InitAuthentication(appCredentials, CALLBACK_URL);

            result.Result = false;
            result.AuthorizationUri = authenticationContext.AuthorizationURL;
            return result;
        }


        private bool TweetCore(ITwitterCredentials userCredential)
        {
            Debug.WriteLine($"{Thread.CurrentThread.ManagedThreadId}");

            ITweet tweet = null;
            Auth.ExecuteOperationWithCredentials(userCredential, () =>
            {
                Debug.WriteLine($"{Thread.CurrentThread.ManagedThreadId}");
                tweet = Tweet.PublishTweet($"Authorized:{DateTime.Now}");
            });

            return tweet != null;
        }

        /// <summary>
        /// APIのテスト
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        [HttpGet]
        public void GetParam(string a, string b)
        {
            Debug.WriteLine($"{a}, {b}");            
        }

        [HttpGet]
        public long GetLatestTweetId()
        {
            var tweetId = 0L;
            var creds = new TwitterCredentials(_twitterApiToken.ConsumerApiKey, _twitterApiToken.ConsumerApiSecretKey, _twitterApiToken.AcessToken, _twitterApiToken.AcessTokenSecret);
            Auth.ExecuteOperationWithCredentials(creds, () =>
            {
                var timeline = Timeline.GetUserTimeline(USER_ID, new UserTimelineParameters()
                {
                    IncludeRTS = true,
                    ExcludeReplies = true,  // Replyを除外
                    IncludeContributorDetails = true,
                    MaximumNumberOfTweetsToRetrieve = 200
                });
                var enumerable = timeline as ITweet[] ?? timeline.ToArray();
                if (enumerable.Any())
                {
                    tweetId = enumerable.First().Id;
                }
            });


            return tweetId;
        }
    }
}