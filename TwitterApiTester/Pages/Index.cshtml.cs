using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using TwitterApiTester.Messages;
using TwitterApiTester.Models;

namespace TwitterApiTester.Pages
{
    public class IndexModel : PageModel
    {
        private readonly TwitterApiToken _twitterApiToken;

        public IndexModel(IOptions<TwitterApiToken> optionsAccessor)
        {
            _twitterApiToken = optionsAccessor.Value;
        }

        //        public List<Repository> Repositories { get; private set; }

        public async Task OnGetAsync()
        {
            //            this.Repositories = await ProcessRequestGitHub();
            // ベアラートークン取得
            using (var twitterClient = new TwitterClient(_twitterApiToken))
            {
                var bearerToken = await twitterClient.GetBearerToken();
                Console.WriteLine(bearerToken.access_token);
            }
        }

//        private static async Task<List<Repository>> ProcessRequestGitHub()
//        {
//            var serializer = new DataContractJsonSerializer(typeof(List<Repository>));
//
//            var client = new HttpClient();
//            client.DefaultRequestHeaders.Accept.Clear();
//            client.DefaultRequestHeaders.Accept.Add(
//                new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
//            client.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");
//
//            var streamTask = client.GetStreamAsync("https://api.github.com/orgs/dotnet/repos");
//            var repositories = serializer.ReadObject(await streamTask) as List<Repository>;
//            return repositories;
//        }
    }
}
