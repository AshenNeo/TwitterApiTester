using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models;
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

            if (IsTwitterSignin)
            {
                // 指定アカウントの最新Tweetを取得してリツイートする。
                var userCreds = AuthFlow.CreateCredentialsFromVerifierCode(_requestTokenVerifier, _authorizationId);


                // 参考：普通にTweetする場合はこう。
                var tweet = Auth.ExecuteOperationWithCredentials(userCreds, () => Tweet.PublishTweet("てすとです"));
            }
            else
            {
                var appCredentials = new TwitterCredentials(_twitterApiToken.ConsumerApiKey, _twitterApiToken.ConsumerApiSecretKey);
                var authenticationContext = AuthFlow.InitAuthentication(appCredentials, CALLBACK_URL);
                AuthorizationUri = authenticationContext.AuthorizationURL;
            }
        }
    }
}
