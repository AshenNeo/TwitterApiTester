namespace TwitterApiTester.Twitter
{
    /// <summary>
    /// タイムライン要素（解析に必要な項目のみ）
    /// </summary>
    public class GetTimelineResponse
    {
        public long id;
        public string text;
        public int retweet_count;
        //        public bool retweeted;        // 取得できない？ retweet_count が1以上でもfalseになる
    }
}
