using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FeedService.Domain
{
    public interface IFeedService : IService
    {
        Task<bool> AddOrUpdateTopicAsync(TopicItem topic);

        Task<bool> AddOrUpdateFeedAsync(FeedItem feed);

        Task<FeedItem> GetFeedAsync(string urlHash);

        Task<IEnumerable<TopicItem>> GetAllTopicsAsync(CancellationToken cancelToken);

        Task<IEnumerable<FeedItem>> GetAllFeedsAsync(CancellationToken cancelToken);
    }
}
