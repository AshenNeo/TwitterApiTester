﻿using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using TwitterApiTester.Twitter;

namespace TwitterApiTester.Pages
{
    /// <summary>
    /// サインイン実行後はこのページに戻る。
    /// サインイン実行後は
    ///     http://127.0.0.1/TwitterApiTester/?oauth_token=MrSl8AAAAAAA8CTYAAABZSNSmOg&oauth_verifier=3onDUzyzo5qt4fh5zu9R6Ur1ffr4SXQw
    /// のように、クエリパラメータでoauth_tokenとoauth_verifierが帰るので、この値でAPIを実行する。
    /// ユーザーがサインインを拒否した場合はクエリパラメータでdeniedが帰る。
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly TwitterApiToken _twitterApiToken;

        public bool IsTwitterSignin { get; set; }
        public string RequestToken { get; set; }

        private string _oauthToken;
        private string _oauthVerifier;


        public IndexModel(IOptions<TwitterApiToken> optionsAccessor)
        {
            _twitterApiToken = optionsAccessor.Value;
        }

        public async Task OnGetAsync()
        {
            if (Request.Query.ContainsKey("oauth_token") && Request.Query.ContainsKey("oauth_verifier"))
            {
                IsTwitterSignin = true;
                _oauthToken = Request.Query["oauth_token"];
                _oauthVerifier = Request.Query["oauth_verifier"];
            }
            else
            {
                IsTwitterSignin = false;
            }

            using (var twitterClient = new TwitterClient(_twitterApiToken, HttpContext.Session, _oauthToken, _oauthVerifier))
            {
                if (twitterClient.HasRequestToken && IsTwitterSignin)
                {
                    var accessToken = await twitterClient.GetAccessToken();
                    Console.WriteLine(accessToken.oauth_token_secret);



                }
                else
                {
                    RequestToken = await twitterClient.GetRequestToken();
                    //                IsTwitterSignin = twitterClient.IsSignIn();
                }

                // タイムラインを取得してリツイート数を集計する
                var timelines = await twitterClient.GetTimeline();
                Console.WriteLine(timelines.Sum(_ => _.retweet_count).ToString());
            }
        }
    }
}
