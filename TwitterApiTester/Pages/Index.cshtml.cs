using System;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Session;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;
using TwitterApiTester.Twitter;

namespace TwitterApiTester.Pages
{
    /// <summary>
    /// サインイン実行後はこのページに戻る。
    /// サインイン実行後は
    ///     http://127.0.0.1/TwitterApiTester/?oauth_token=MrSl8AAAAAAA8CTYAAABZSNSmOg&oauth_verifier=3onDUzyzo5qt4fh5zu9R6Ur1ffr4SXQw
    /// のように、クエリパラメータでoauth_tokenとoauth_verifierとauthorization_idが帰るので、この値でAPIを実行する。
    /// ユーザーがサインインを拒否した場合はクエリパラメータでdeniedが帰る。
    /// </summary>
    public class IndexModel : PageModel
    {
        private const string CALLBACK_URL = "http://127.0.0.1/TwitterApiTester/";
        private const string QUERY_PARAM_OAUTH_TOKEN = "oauth_token";
        private const string QUERY_PARAM_OAUTH_VERIFIER = "oauth_verifier";
        private const string QUERY_PARAM_AUTHORIZATION_ID = "authorization_id";
        private const long USER_ID = 133684052;   // サントリー公式のUserID

        private const string SESSION_USER_ACCESS_TOKEN = "TwitterUserAccessToken";
        private const string SESSION_USER_ACCESS_TOKEN_SECRET = "TwitterUserAccessTokenSecret";
//        private const string SESSION_RETWEET_


        private readonly TwitterApiToken _twitterApiToken;

        public bool IsTwitterSignin { get; set; }

        public string AuthorizationUri { get; set; }


        public string RequestToken { get; set; }

        private string _requestToken;
        private string _requestTokenVerifier;
        private string _authorizationId;


        public IndexModel(IOptions<TwitterApiToken> optionsAccessor)
        {
            _twitterApiToken = optionsAccessor.Value;
        }

        public async Task OnGetAsync()
        {
            // ユーザーアクセストークンが保存されている場合はそのまま使う
            // 認証切れ等でトークンが無効な場合、Tweet実行結果が NULL となる。
            var userAccessToken = HttpContext.Session.GetString(SESSION_USER_ACCESS_TOKEN);
            var userAccessTokenSecret = HttpContext.Session.GetString(SESSION_USER_ACCESS_TOKEN_SECRET);
            if (!string.IsNullOrEmpty(userAccessToken) && !string.IsNullOrEmpty(userAccessTokenSecret))
            {
                var userCreds = Auth.SetUserCredentials(_twitterApiToken.ConsumerApiKey,
                    _twitterApiToken.ConsumerApiSecretKey, userAccessToken, userAccessTokenSecret);

                Auth.ExecuteOperationWithCredentials(userCreds, () =>
                {
                    var tweet = Tweet.PublishTweet($"Authorized:{DateTime.Now}");
                    if (tweet != null)
                    {
                        IsTwitterSignin = true;
                        return;
                    }
                });
            }

            // 有効なアクセストークンが保存されていない場合は認証から実行する。
            if (Request.Query.ContainsKey(QUERY_PARAM_OAUTH_TOKEN) && Request.Query.ContainsKey(QUERY_PARAM_OAUTH_VERIFIER) && Request.Query.ContainsKey(QUERY_PARAM_AUTHORIZATION_ID))
            {
                IsTwitterSignin = true;
                _requestToken = Request.Query[QUERY_PARAM_OAUTH_TOKEN];
                _requestTokenVerifier = Request.Query[QUERY_PARAM_OAUTH_VERIFIER];
                _authorizationId = Request.Query[QUERY_PARAM_AUTHORIZATION_ID];
            }
            else
            {
                IsTwitterSignin = false;
            }

            var appCredentials = new TwitterCredentials(_twitterApiToken.ConsumerApiKey, _twitterApiToken.ConsumerApiSecretKey);
            Auth.SetCredentials(appCredentials);
            if (IsTwitterSignin)
            {
                // 指定アカウントの最新Tweetを取得してリツイートする。
                var userCreds = AuthFlow.CreateCredentialsFromVerifierCode(_requestTokenVerifier, _authorizationId);

                // ユーザーのアクセストークンとシークレットを保存
                HttpContext.Session.SetString(SESSION_USER_ACCESS_TOKEN, userCreds.AccessToken);
                HttpContext.Session.SetString(SESSION_USER_ACCESS_TOKEN_SECRET, userCreds.AccessTokenSecret);


                Auth.ExecuteOperationWithCredentials(userCreds, () =>
                {
                    var timeline = Timeline.GetUserTimeline(USER_ID, new UserTimelineParameters()
                    {
                        IncludeRTS = true,
                        ExcludeReplies = true,  // Replyを除外
                        IncludeContributorDetails = true
                    });
                    var enumerable = timeline as ITweet[] ?? timeline.ToArray();
                    if (enumerable.Any())
                    {
                        Tweet.PublishRetweet(enumerable.First());
                    }
                });
            }
            else
            {
                var authenticationContext = AuthFlow.InitAuthentication(appCredentials, CALLBACK_URL);
                AuthorizationUri = authenticationContext.AuthorizationURL;
            }
        }
    }
}
