using Common;
using FeedService.Domain;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Subscription.Domain;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using UserService.Models;

namespace UserService.Controllers
{
    [RoutePrefix("api/v1/subscription")]
    public class SubscriptionController : ApiController
    {
        private const string FeedServiceName = "FeedService";
        private const string SubscriptionServiceName = "SubscriptionService";
        private static FabricClient fabricClient = new FabricClient();

        [HttpGet]
        [Route("gettopics")]
        public async Task<IEnumerable<TopicItem>> GetTopics()
        {
            var builder = new ServiceUriBuilder(FeedServiceName);
            var allPartitions = await fabricClient.QueryManager.GetPartitionListAsync(builder.ToUri());
            var allTopics = new List<TopicItem>();
            foreach (var currentPartition in allPartitions)
            {
                long minKey = (currentPartition.PartitionInformation as Int64RangePartitionInformation).LowKey;
                var feedServiceClient = ServiceProxy.Create<IFeedService>(builder.ToUri(), new ServicePartitionKey(minKey));
                var result = await feedServiceClient.GetAllTopicsAsync(CancellationToken.None);
                if (result != null)
                {
                    allTopics.AddRange(result);
                }
            }

            return allTopics;
        }

        [HttpGet]
        [Route("getfeeds")]
        public async Task<IEnumerable<FeedItem>> GetAllFeeds()
        {
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

        [HttpPost]
        [Route("create")]
        public Task<bool> CreateSubscription([FromBody] UserSubscription userSubscription)
        {
            var subscriptionItem = new SubscriptionItem() { UserEmail = userSubscription.Email.ToLower(), Feeds = new List<FeedItem>() };

            var builder = new ServiceUriBuilder(SubscriptionServiceName);
            var subscriptionServiceClient = ServiceProxy.Create<ISubscriptionService>(builder.ToUri(), subscriptionItem.GetPartitionKey());

            foreach (var feed in userSubscription.Feeds)
            {
                subscriptionItem.Feeds.Add(feed);
            }

            try
            {
                return subscriptionServiceClient.AddOrUpdateSubscriptionAsync(subscriptionItem);
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message("Web Service: Exception creating {0}: {1}", subscriptionItem, ex);
                throw;
            }
        }


        [HttpPost]
        [Route("get")]
        public async Task<SubscriptionItem> GetSubscription([FromBody] SubscriptionItem subcriptionItem)
        {
            var builder = new ServiceUriBuilder(SubscriptionServiceName);
            var subscriptionServiceClient = ServiceProxy.Create<ISubscriptionService>(builder.ToUri(), subcriptionItem.GetPartitionKey());
            return await subscriptionServiceClient.GetSubscriptionAsync(subcriptionItem.UserEmail, CancellationToken.None);
        }
    }
}
