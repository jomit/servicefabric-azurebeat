using Common;
using FeedService.Domain;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Query;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace AdminService.Controllers
{
    [RoutePrefix("api/v1/topic")]
    public class TopicController : ApiController
    {
        private const string FeedServiceName = "FeedService";
        private static FabricClient fabricClient = new FabricClient();

        [HttpPost]
        [Route("create")]
        public Task<bool> CreateTopic([FromBody] TopicItem topic)
        {
            var feedTopic = new TopicItem() { Name = topic.Name };

            var builder = new ServiceUriBuilder(FeedServiceName);
            var feedServiceClient = ServiceProxy.Create<IFeedService>(builder.ToUri(), feedTopic.GetPartitionKey());
           
            try
            {
                return feedServiceClient.AddOrUpdateTopicAsync(feedTopic);
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message("Web Service: Exception creating {0}: {1}", feedTopic, ex);
                throw;
            }
        }

        [HttpGet]
        [Route("getall")]
        public async Task<IEnumerable<TopicItem>> GetAllTopics()
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
    }
}
