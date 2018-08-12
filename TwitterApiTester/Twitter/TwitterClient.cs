using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace TwitterApiTester.Twitter
{
    /// <summary>
    /// Twitterクライアント
    /// </summary>
    public class TwitterClient : HttpClient
    {
        // TODO:定数は後でconfigに移動する
        private const string USER_ID = "364233642";   // Gloops公式のUserID

        private const string SESSION_DATA_REQUEST_TOKEN = "OAUTH_TOKEN";
        private const string SESSION_DATA_REQUEST_TOKEN_SECRET = "OAUTH_TOKEN_SECRET";

        /// <summary>
        /// API実行時のコールバックURL
        /// Twitterのアプリケーション設定（ttps://developer.twitter.com/en/apps/xxxx）で指定したURLと完全一致しないと403になるので注意
        /// アプリケーション設定でコールバックURLを登録していない場合は "oob" を指定する。
        /// </summary>
        private const string CALLBACK_URL = "http://127.0.0.1/TwitterApiTester/";
//        private const string CALLBACK_URL = "oob";      // デスクトップアプリケーションの場合はこの値を指定する

        private Random random = new Random();

        private readonly TwitterApiToken _twitterApiToken;
        private readonly ISession _session;

        private string _oauthToken;
        private string _oauthVerifier;


        public enum MethodType
        {
            Get,
            Post
        }


        public TwitterClient(TwitterApiToken twitterApiToken, ISession session, string oauthToken = null, string oauthVerifier = null)
        {
            _twitterApiToken = twitterApiToken;
            _session = session;
            _oauthToken = oauthToken;
            _oauthVerifier = oauthVerifier;
        }

//        /// <summary>
//        /// サインイン済みかチェックする。
//        /// </summary>
//        /// <returns></returns>
//        public bool IsSignIn()
//        {
//            return false;
//        }

        /// <summary>
        /// リクエストトークンを取得する
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetRequestToken()
        {
            // セッション情報に保存されている場合はその値を使用する。
            // 存在しない場合は取得する。
            var oauthToken = _session.GetString(SESSION_DATA_REQUEST_TOKEN);
            if (string.IsNullOrEmpty(oauthToken))
            {
                var timeStamp = GetTimeStamp();
                var oAuthHeaderParams = new SortedDictionary<string, string>
                {
                    { "oauth_callback", HttpUtility.UrlEncode(CALLBACK_URL) },
                    { "oauth_consumer_key", _twitterApiToken.ConsumerApiKey },
                    { "oauth_signature_method", "HMAC-SHA1" },
                    { "oauth_timestamp", timeStamp },
                    { "oauth_nonce", GenerateNonce() },
                    { "oauth_version", "1.0" },
                };

                var signature = CreateOAuthSignature(MethodType.Get, TwitterApi.GetRequestToken, "", oAuthHeaderParams);
                var response = await GetStringAsync($"{TwitterApi.GetRequestToken}?{BuildQueryString(oAuthHeaderParams)}&oauth_signature={Uri.EscapeDataString(signature)}");
                var tokens = HttpUtility.ParseQueryString(response);

                oauthToken = tokens["oauth_token"];

                _session.SetString(SESSION_DATA_REQUEST_TOKEN, oauthToken);
                _session.SetString(SESSION_DATA_REQUEST_TOKEN_SECRET, tokens["oauth_token_secret"]);
            }

            return oauthToken;
        }

        /// <summary>
        /// アクセストークンを取得する
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// リツイート等、ユーザーアカウントで行う操作にはこちらを使用する。
        /// </remarks>
        public async Task<GetAccessTokenResponse> GetAccessToken()
        {
            var timeStamp = GetTimeStamp();
            var oAuthHeaderParams = new SortedDictionary<string, string>
            {
                { "oauth_consumer_key", _twitterApiToken.ConsumerApiKey },
                { "oauth_nonce", GenerateNonce() },
                { "oauth_signature_method", "HMAC-SHA1" },
                { "oauth_timestamp", timeStamp },
                { "oauth_token", _oauthToken },
                { "oauth_version", "1.0" },
            };

            var requestTokenSecret = _session.GetString(SESSION_DATA_REQUEST_TOKEN_SECRET);
            var signature = CreateOAuthSignature(MethodType.Post, TwitterApi.GetAccessToken, requestTokenSecret, oAuthHeaderParams);

            oAuthHeaderParams.AddUrlEncodedItem("oauth_signature", signature);
            oAuthHeaderParams["oauth_token"] = HttpUtility.UrlEncode(_oauthToken);

            using (var content = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                {"oauth_verifier", _oauthVerifier}
            }))
            {
                DefaultRequestHeaders.Accept.Clear();
                DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "OAuth",
                    BuildQueryString(oAuthHeaderParams));

                var response = await PostAsync(TwitterApi.GetAccessToken, content);
                var responseBody = await response.Content.ReadAsStringAsync();
                var tokens = HttpUtility.ParseQueryString(responseBody);
                return new GetAccessTokenResponse()
                {
                    oauth_token = tokens["oauth_token"],
                    oauth_token_secret = tokens["oauth_token_secret"],
                    user_id = tokens["user_id"],
                    screen_name = tokens["screen_name"],
                };
            }
        }



        //        /// <summary>
        //        /// サインイン
        //        /// </summary>
        //        /// <returns></returns>
        //        /// <remarks>
        //        /// リツイート等、ユーザーアカウントで行う操作の前に実行する。
        //        /// </remarks>
        //        public async Task SignIn()
        //        {
        //        }

        public async Task<List<GetTimelineResponse>> GetTimeline()
        {
            var bearerToken = await GetBearerToken();

            var queryString = BuildQueryString(new Dictionary<string, string>()
            {
                { "user_id", USER_ID },
                { "count", "200" },         // 最新から200件。 200件以上ある場合は since_id を指定しないとダメかもだ
                { "trim_user", "" },
                { "exclude_replies", "true" },
                { "contributor_details", "false" },
                { "include_rts", "false" },
            });

            DefaultRequestHeaders.Accept.Clear();
            DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                bearerToken.access_token);

            var response = await GetAsync($"{TwitterApi.GetUserTimeline}?{queryString}");
            var result = await response.Content.ReadAsStreamAsync();
            var serializer = new DataContractJsonSerializer(typeof(List<GetTimelineResponse>));
            return serializer.ReadObject(result) as List<GetTimelineResponse>;
        }

        /// <summary>
        /// ベアラートークンを取得する
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// タイムラインの取得にはこちらを使用する。
        /// </remarks>
        private async Task<GetBearerTokenResponse> GetBearerToken()
        {
            using (var content = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                {"grant_type", "client_credentials"}
            }))
            {
                DefaultRequestHeaders.Accept.Clear();
                DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_twitterApiToken.ConsumerApiKey}:{_twitterApiToken.ConsumerApiSecretKey}")));

                var response = await PostAsync(TwitterApi.GetBearerToken, content);
                var result = await response.Content.ReadAsStreamAsync();
                var serializer = new DataContractJsonSerializer(typeof(GetBearerTokenResponse));
                return serializer.ReadObject(result) as GetBearerTokenResponse;
            }
        }


        /// <summary>
        /// ユーザーのアクセストークンでOAuth署名を作成する
        /// </summary>
        /// <returns></returns>
        private string CreateOAuthSignature(MethodType methodType, string requestUrl, string requestTokenSecret, SortedDictionary<string, string> oAuthHeaderParams)
        {
            // 署名キー作成
            var apiSecretKey = HttpUtility.UrlEncode(_twitterApiToken.ConsumerApiSecretKey);
            var accessTokenSecret = HttpUtility.UrlEncode(requestTokenSecret);
            var signatureKey = $"{apiSecretKey}&{accessTokenSecret}";

            // 署名データ作成
            var requestMethod = methodType == MethodType.Get ? "GET" : "POST";
            var signatureData = $"{requestMethod}&{Uri.EscapeDataString(requestUrl)}&{Uri.EscapeDataString(BuildQueryString(oAuthHeaderParams))}";

            // HMAC-SHA1、Base64
            var signatureKeyBites = Encoding.UTF8.GetBytes(signatureKey);
            var signatureDataBites = Encoding.UTF8.GetBytes(signatureData);
            var hmac = new System.Security.Cryptography.HMACSHA1(signatureKeyBites);
            var hmacHashBites = hmac.ComputeHash(signatureDataBites);

            return Convert.ToBase64String(hmacHashBites);
        }

        private static string BuildQueryString(IEnumerable<KeyValuePair<string, string>> nameValueCollection)
        {
            using (var content = new FormUrlEncodedContent(nameValueCollection))
            {
                return content.ReadAsStringAsync().Result;
            }
        }

        private string GenerateNonce()
        {
            // Just a simple implementation of a random number between 123400 and 9999999 
            return random.Next(123400, 9999999).ToString();
        }

        private static string GetTimeStamp()
        {
            var sinceEpoch = DateTime.UtcNow - new DateTime(1970, 1, 1);
            return Math.Round(sinceEpoch.TotalSeconds).ToString();
        }
    }
}
