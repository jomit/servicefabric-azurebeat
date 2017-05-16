using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Subscription.Domain;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using FeedService.Domain;
using Common;
using Microsoft.ServiceFabric.Services.Client;

namespace SubscriptionService
{
    internal sealed class SubscriptionService : StatefulService, ISubscriptionService
    {
        internal const string SubscriptionServiceType = "SubscriptionServiceType";
        private const string SubscriptionsDictionaryName = "Subscriptions";
        private const string PostsDictionaryName = "Posts";
        private const string FeedServiceName = "FeedService";

        public SubscriptionService(StatefulServiceContext context)
            : base(context)
        { }

        public async Task<bool> AddOrUpdateSubscriptionAsync(SubscriptionItem subscriptionItem)
        {
            var subscriptions = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, SubscriptionItem>>(SubscriptionsDictionaryName);
            using (var transaction = this.StateManager.CreateTransaction())
            {
                ConditionalValue<SubscriptionItem> item = await subscriptions.TryGetValueAsync(transaction, subscriptionItem.UserEmail);
                var currentSubscription = subscriptionItem;
                if (item.HasValue)
                {
                    ServiceEventSource.Current.ServiceMessage(this, "Found subscription for: {0}", currentSubscription.UserEmail);
                    currentSubscription = item.Value;
                    subscriptionItem.Feeds.ForEach(feedPost =>
                    {
                        if (!currentSubscription.Feeds.Any(f => f.Url.ToString().ToLower() == feedPost.Url.ToString().ToLower()))
                        {
                            currentSubscription.Feeds.Add(feedPost);
                        }
                    });
                }

                await subscriptions.AddOrUpdateAsync(transaction, currentSubscription.UserEmail, currentSubscription, (key, oldValue) => currentSubscription);
                await transaction.CommitAsync();

                if (item.HasValue)
                {
                    ServiceEventSource.Current.ServiceMessage(this, "Updated subscription for: {0}", currentSubscription.UserEmail);
                }
                else
                {
                    ServiceEventSource.Current.ServiceMessage(this, "Created subscription for: {0}", currentSubscription.UserEmail);
                }
            }
            return true;
        }

        public async Task<SubscriptionItem> GetSubscriptionAsync(string emailId, CancellationToken cancelToken)
        {
            var subscriptionItems = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, SubscriptionItem>>(SubscriptionsDictionaryName);
            var subscriptions = new List<SubscriptionItem>();

            ServiceEventSource.Current.Message("Called GetSubscriptionAsync");
            using (var transaction = this.StateManager.CreateTransaction())
            {
                var item = await subscriptionItems.TryGetValueAsync(transaction, emailId);
                if (item.HasValue)
                {
                    var currentSubscription = item.Value;
                    var builder = new ServiceUriBuilder(FeedServiceName);

                    foreach (var feed in currentSubscription.Feeds)
                    {
                        var feedServiceClient = ServiceProxy.Create<IFeedService>(builder.ToUri(), feed.GetPartitionKey());
                        var result = await feedServiceClient.GetFeedAsync(feed.GetUrlHash());
                        feed.LastUpdatedTimeStamp = result.LastUpdatedTimeStamp;
                        feed.Posts = result.Posts;
                        feed.Title = result.Title;
                    }
                    return currentSubscription;
                }
                return null;
            }
        }

        public async Task<IEnumerable<SubscriptionItem>> GetAllSubscriptionsAsync(CancellationToken cancelToken)
        {
            var subscriptionItems = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, SubscriptionItem>>(SubscriptionsDictionaryName);
            var subscriptions = new List<SubscriptionItem>();
            var builder = new ServiceUriBuilder(FeedServiceName);

            ServiceEventSource.Current.Message("Called GetAllSubscriptionsAsync to return list of all subscriptions ");

            using (var transaction = this.StateManager.CreateTransaction())
            {
                ServiceEventSource.Current.Message("Generating subscription item for {0} items", await subscriptionItems.GetCountAsync(transaction));
                var subEnumerator = (await subscriptionItems.CreateEnumerableAsync(transaction)).GetAsyncEnumerator();
                while (await subEnumerator.MoveNextAsync(cancelToken))
                {
                    var currentSubscription = subEnumerator.Current.Value;
                    foreach (var feed in currentSubscription.Feeds)
                    {
                        var feedServiceClient = ServiceProxy.Create<IFeedService>(builder.ToUri(), feed.GetPartitionKey());
                        var result = await feedServiceClient.GetFeedAsync(feed.GetUrlHash());
                        feed.LastUpdatedTimeStamp = result.LastUpdatedTimeStamp;
                        feed.Posts = result.Posts;
                        feed.Title = result.Title;
                    }
                    subscriptions.Add(currentSubscription);
                }
            }
            return subscriptions;
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new[]
           {
                new ServiceReplicaListener(context => this.CreateServiceRemotingListener(context))
            };
        }
    }
}
