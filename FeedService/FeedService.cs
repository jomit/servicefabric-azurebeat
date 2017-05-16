using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using FeedService.Domain;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Common;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Client;
using System.Xml;
using System.ServiceModel.Syndication;

namespace FeedService
{
    internal sealed class FeedService : StatefulService, IFeedService
    {
        internal const string FeedServiceType = "FeedServiceType";
        private const string FeedItemDictionaryName = "Feeds";
        private const string TopicsDictionaryName = "Topics";
        private const int SyncFeedsDealyInMinutes = 2;  //TODO Jomit => Need to make it configurable

        public FeedService(StatefulServiceContext context)
            : base(context)
        { }

        public async Task<bool> AddOrUpdateTopicAsync(TopicItem topic)
        {
            var topics = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, TopicItem>>(TopicsDictionaryName);
            using (var transaction = this.StateManager.CreateTransaction())
            {
                await topics.AddOrUpdateAsync(transaction, topic.Name.ToLower(), topic, (key, oldValue) => topic);
                await transaction.CommitAsync();
                ServiceEventSource.Current.ServiceMessage(this, "Updated topic item: {0}", topic);
            }
            return true;
        }

        public async Task<bool> AddOrUpdateFeedAsync(FeedItem feed)
        {
            var feedItems = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, FeedItem>>(FeedItemDictionaryName);
            using (var transaction = this.StateManager.CreateTransaction())
            {
                var result = await feedItems.TryGetValueAsync(transaction, feed.GetUrlHash(), LockMode.Default);
                FeedItem currentFeed = feed;
                if (result.HasValue)
                {
                    currentFeed = result.Value;
                    currentFeed.Topics = feed.Topics;
                }
                await feedItems.AddOrUpdateAsync(transaction, feed.GetUrlHash(), currentFeed, (key, oldValue) => currentFeed);
                await transaction.CommitAsync();
                ServiceEventSource.Current.ServiceMessage(this, "Updated feed item: {0}", feed);
            }
            return true;
        }

        public async Task<FeedItem> GetFeedAsync(string urlHash)
        {
            var feedItems = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, FeedItem>>(FeedItemDictionaryName);
            using (var transaction = this.StateManager.CreateTransaction())
            {
                var result = await feedItems.TryGetValueAsync(transaction, urlHash, LockMode.Default);
                return result.HasValue ? result.Value : null;
            }
        }

        public async Task<IEnumerable<TopicItem>> GetAllTopicsAsync(CancellationToken cancelToken)
        {
            var topicItems = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, TopicItem>>(TopicsDictionaryName);
            var topics = new List<TopicItem>();

            ServiceEventSource.Current.Message("Called GetAllTopicsAsync to return list of all topics");

            using (var transaction = this.StateManager.CreateTransaction())
            {
                //ServiceEventSource.Current.Message("Generating topic item for {0} items", await topicItems.GetCountAsync(transaction));
                var feedEnumerator = (await topicItems.CreateEnumerableAsync(transaction)).GetAsyncEnumerator();
                while (await feedEnumerator.MoveNextAsync(cancelToken))
                {
                    topics.Add(feedEnumerator.Current.Value);
                }
            }
            return topics;
        }

        public async Task<IEnumerable<FeedItem>> GetAllFeedsAsync(CancellationToken cancelToken)
        {
            var feedItems = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, FeedItem>>(FeedItemDictionaryName);
            var feeds = new List<FeedItem>();

            ServiceEventSource.Current.Message("Called GetAllFeedsAsync to return list of all feeds");

            using (var transaction = this.StateManager.CreateTransaction())
            {
                //TODO try removing the count and see the perf improve
                //ServiceEventSource.Current.Message("Generating feed item view for {0} items", await feedItems.GetCountAsync(transaction));
                var feedEnumerator = (await feedItems.CreateEnumerableAsync(transaction)).GetAsyncEnumerator();
                while (await feedEnumerator.MoveNextAsync(cancelToken))
                {
                    feeds.Add(feedEnumerator.Current.Value);
                }
            }
            return feeds;
        }

        protected override Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                ServiceEventSource.Current.ServiceMessage(this, "Inside RunAsync for Post Service");

                return Task.WhenAll(this.SyncFeeds(cancellationToken));
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceMessage(this, "RunAsync Failed, {0}", e);
                throw;
            }
        }

        private async Task SyncFeeds(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var feedItems = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, FeedItem>>(FeedItemDictionaryName);
                var allFeeds = await this.GetAllFeedsAsync(CancellationToken.None);

                var partitionKey = (this.Partition.PartitionInfo as Int64RangePartitionInformation).LowKey;
                ServiceEventSource.Current.ServiceMessage(this, String.Format("[{0}] - {1} => Fetching posts for {1} feeds...", partitionKey, DateTime.Now, allFeeds.Count()));

                foreach (var oldFeed in allFeeds.Where(f => f.DisableSync == false))
                {
                    try
                    {
                        var reader = XmlReader.Create(oldFeed.Url.ToString());
                        var syndicationFeed = SyndicationFeed.Load(reader);

                        var newFeed = new FeedItem()
                        {
                            Url = oldFeed.Url,
                            DisableSync = oldFeed.DisableSync,
                            Topics = oldFeed.Topics
                        };
                        newFeed.Title = syndicationFeed.Title.Text;
                        newFeed.Posts = syndicationFeed.Items.Select(sf => new PostItem
                        {
                            Title = sf.Title.Text,
                            Summary = sf.Summary.Text,
                            Url = sf.Links.First().Uri
                        }).ToList();
                        newFeed.LastUpdatedTimeStamp = syndicationFeed.LastUpdatedTime;

                        using (var transaction = this.StateManager.CreateTransaction())
                        {
                            await feedItems.TryUpdateAsync(transaction, oldFeed.GetUrlHash(), newFeed, oldFeed);
                            await transaction.CommitAsync();
                        }

                        ServiceEventSource.Current.ServiceMessage(this, String.Format("Updated posts for {0} ", newFeed.Url));
                    }
                    catch (Exception ex)
                    {
                        ServiceEventSource.Current.ServiceMessage(this, "Failed to fetch posts for {0}", oldFeed.Url.ToString(), ex.ToString());
                    }
                }


                await Task.Delay(TimeSpan.FromMinutes(SyncFeedsDealyInMinutes), cancellationToken);
            }
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new[] {
                new ServiceReplicaListener(context => this.CreateServiceRemotingListener(context))
            };
        }
    }
}
