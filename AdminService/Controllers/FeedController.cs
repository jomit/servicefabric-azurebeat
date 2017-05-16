using Common;
using FeedService.Domain;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace AdminService.Controllers
{
    [RoutePrefix("api/v1/feed")]
    public class FeedController : ApiController
    {
        private const string FeedServiceName = "FeedService";
        private static FabricClient fabricClient = new FabricClient();

        [HttpPost]
        [Route("create")]
        public Task<bool> CreateFeed([FromBody] FeedItem feed)
        {
            var feedItem = new FeedItem()
            {
                Url = feed.Url,
                Topics = feed.Topics
            };

            var builder = new ServiceUriBuilder(FeedServiceName);
            var feedServiceClient = ServiceProxy.Create<IFeedService>(builder.ToUri(), feedItem.GetPartitionKey());
            
            try
            {
                return feedServiceClient.AddOrUpdateFeedAsync(feedItem);
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message("Web Service: Exception creating {0}: {1}", feedItem, ex);
                throw;
            }
        }

        [HttpGet]
        [Route("getall")]
        public async Task<IEnumerable<FeedItem>> GetAllFeeds()
        {
            //TODO Jomit => Use the InvokeWithRetryAsync method 
            //example -> See 'Count' method here https://github.com/Azure-Samples/service-fabric-dotnet-getting-started/blob/master/Services/WordCount/WordCount.WebService/Controllers/DefaultController.cs

            var builder = new ServiceUriBuilder(FeedServiceName);
            var allPartitions = await fabricClient.QueryManager.GetPartitionListAsync(builder.ToUri());
            var allFeeds = new List<FeedItem>();
            foreach (var currentPartition in allPartitions)
            {
                long minKey = (currentPartition.PartitionInformation as Int64RangePartitionInformation).LowKey;
                var feedServiceClient = ServiceProxy.Create<IFeedService>(builder.ToUri(), new ServicePartitionKey(minKey));
                var result = await feedServiceClient.GetAllFeedsAsync(CancellationToken.None);
                if (result != null)
                {
                    allFeeds.AddRange(result);
                }
            }

            return allFeeds;
        }
    }
}
