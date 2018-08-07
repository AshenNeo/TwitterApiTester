using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;
using TwitterApiTester.Twitter;

namespace TwitterApiTester.Pages
{
    public class IndexModel : PageModel
    {
        private readonly TwitterApiToken _twitterApiToken;

        public IndexModel(IOptions<TwitterApiToken> optionsAccessor)
        {
            _twitterApiToken = optionsAccessor.Value;
        }

        public async Task OnGetAsync()
        {
            // タイムラインを取得してリツイート数を集計する
            using (var twitterClient = new TwitterClient(_twitterApiToken))
            {
                var timelines = await twitterClient.GetTimeline();
                Console.WriteLine(timelines.Sum(_ => _.retweet_count).ToString());
            }
        }
    }
}
