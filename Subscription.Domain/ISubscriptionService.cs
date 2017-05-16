using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Subscription.Domain
{
    public interface ISubscriptionService : IService
    {
        Task<bool> AddOrUpdateSubscriptionAsync(SubscriptionItem subscriptionItem);

        Task<SubscriptionItem> GetSubscriptionAsync(string emailId, CancellationToken cancelToken);

        Task<IEnumerable<SubscriptionItem>> GetAllSubscriptionsAsync(CancellationToken cancelToken);

    }
}
