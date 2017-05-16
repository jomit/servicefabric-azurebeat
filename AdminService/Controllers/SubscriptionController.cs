using Common;
using FeedService.Domain;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Subscription.Domain;
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
    [RoutePrefix("api/v1/subscription")]
    public class SubscriptionController : ApiController
    {
        private const string SubscriptionServiceName = "SubscriptionService";
        private static FabricClient fabricClient = new FabricClient();

        [HttpGet]
        [Route("getall")]
        public async Task<IEnumerable<SubscriptionItem>> GetAllSubscriptions()
        {
            var builder = new ServiceUriBuilder(SubscriptionServiceName);
            var allPartitions = await fabricClient.QueryManager.GetPartitionListAsync(builder.ToUri());
            var allSubscriptions = new List<SubscriptionItem>();
            foreach (var currentPartition in allPartitions)
            {
                long minKey = (currentPartition.PartitionInformation as Int64RangePartitionInformation).LowKey;
                var subServiceClient = ServiceProxy.Create<ISubscriptionService>(builder.ToUri(), new ServicePartitionKey(minKey));
                var result = await subServiceClient.GetAllSubscriptionsAsync(CancellationToken.None);
                if (result != null)
                {
                    allSubscriptions.AddRange(result);
                }
            }

            return allSubscriptions;
        }
    }
}
