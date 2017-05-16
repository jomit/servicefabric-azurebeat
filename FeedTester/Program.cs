using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.Collections;

namespace FeedTester
{
    public class Program
    {
        static void Main(string[] args)
        {
            var feedUrl = new Uri("http://azure.microsoft.com/en-in/updates/feed/");
            //var feedUrl = new Uri("https://news.ycombinator.com/rss");
            //var feedUrl = new Uri("http://feeds.feedburner.com/Jomit");

            var feedUrlList = GetAllFeedUrls(feedUrl).GetAwaiter().GetResult();
            var allPosts = new List<PostItem>();
            foreach (var url in feedUrlList)
            {
                var currentPosts = GetPosts(url);
                allPosts.AddRange(currentPosts);
            }
            var uniquePosts = allPosts.GroupBy(p => p.Url).Select(group => group.First());
            Console.WriteLine(uniquePosts.Count());

            //foreach (var item in uniquePosts)
            //{
            //    Console.WriteLine(item.Title);
            //}

            Console.ReadLine();
        }

        static async Task<List<Uri>> GetAllFeedUrls(Uri mainFeedUri)
        {
            var archieveUrl = "http://web.archive.org/cdx/search/cdx?url=" + mainFeedUri.ToString() + "&output=json&from=" + (DateTime.Now.Year - 10) + "&to=" + DateTime.Now.Year;
            var feedUrlList = new List<Uri>();
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(archieveUrl);
                if (response.IsSuccessStatusCode)
                {
                    dynamic data = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
                    for (int i = 1; i < data.Count; i++)
                    {
                        var date = data[i][1];
                        var originalFeedUrl = data[i][2];
                        feedUrlList.Add(new Uri("http://web.archive.org/web/" + date + "/" + originalFeedUrl));
                    }
                }
            }
            return feedUrlList;
        }

        static IEnumerable<PostItem> GetPosts(Uri feedUrl)
        {
            var reader = XmlReader.Create(feedUrl.ToString());
            var syndicationFeed = SyndicationFeed.Load(reader);

            return syndicationFeed.Items.Select(si => new PostItem
            {
                Title = si.Title.Text,
                Url = si.Links.First().Uri,
                Summary = si.Summary.Text
            });
        }
    }


    public class PostItem
    {
        public Uri Url { get; set; }

        public string Title { get; set; }

        public string Summary { get; set; }
    }
}
