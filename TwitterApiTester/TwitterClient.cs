using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using TwitterApiTester.Messages;
using TwitterApiTester.Models;

namespace TwitterApiTester.Pages
{
    /// <summary>
    /// Twitterクライアント
    /// </summary>
    public class TwitterClient : HttpClient
    {
        private readonly TwitterApiToken _twitterApiToken;

        public enum MethodType
        {
            Get,
            Post
        }


        public TwitterClient(TwitterApiToken twitterApiToken)
        {
            _twitterApiToken = twitterApiToken;
        }

        /// <summary>
        /// サインイン
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// リツイート等、ユーザーアカウントで行う操作の前に実行する。
        /// </remarks>
        public async Task SignIn()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// ベアラートークンを取得する
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// タイムラインの取得にはこちらを使用する。
        /// </remarks>
        public async Task<GetBearerTokenResponse> GetBearerToken()
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "grant_type", "client_credentials" }
            });

            DefaultRequestHeaders.Accept.Clear();
            DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_twitterApiToken.ConsumerApiKey}:{_twitterApiToken.ConsumerApiSecretKey}")));

            var response = await PostAsync($"https://api.twitter.com/oauth2/token", content);
            var result = await response.Content.ReadAsStreamAsync();
            var serializer = new DataContractJsonSerializer(typeof(GetBearerTokenResponse));
            return serializer.ReadObject(result) as GetBearerTokenResponse;
        }

        /// <summary>
        /// アクセストークンを取得する
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// リツイート等、ユーザーアカウントで行う操作にはこちらを使用する。
        /// </remarks>
        private async Task GetAccessToken()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// リクエストトークンを取得する
        /// </summary>
        /// <returns></returns>
        private async Task GetRequestToken()
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// ユーザーのアクセストークンでOAuth署名を作成する
        /// </summary>
        /// <returns></returns>
        private string CreateOAuthSignature(MethodType methodType, string url, Dictionary<string, string> parameters)
        {
            // 署名キー作成
            var apiSecretKey = HttpUtility.UrlEncode(_twitterApiToken.ConsumerApiSecretKey);
            var accessTokenSecret = HttpUtility.UrlEncode("");
            var signatureKey = $"{apiSecretKey}&{accessTokenSecret}";

            // 署名データ作成
            var requestMethod = HttpUtility.UrlEncode(methodType == MethodType.Get ? "GET" : "POST");
            var requestUrl = HttpUtility.UrlEncode(url);

            var requestParamBuilder = new StringBuilder();
            foreach (var param in parameters)
            {
                if (requestParamBuilder.Length > 0) requestParamBuilder.Append("&");
                requestParamBuilder.Append($"{param.Key}={HttpUtility.UrlEncode(param.Value)}");
            }

            var requestParameters = HttpUtility.UrlEncode(requestParamBuilder.ToString());

            var signatureData = $"{requestMethod}&{requestUrl}&{requestParameters}";

            // HMAC-SHA1、Base64
            var signatureKeyBites = System.Text.Encoding.UTF8.GetBytes(signatureKey);
            var signatureDataBites = System.Text.Encoding.UTF8.GetBytes(signatureData);
            var hmac = new System.Security.Cryptography.HMACSHA1(signatureKeyBites);
            var hmacHashBites = hmac.ComputeHash(signatureDataBites);

            return Convert.ToBase64String(hmacHashBites);
        }
    }
}
