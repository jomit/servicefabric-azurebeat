using Microsoft.ServiceFabric.Services.Client;
using System.Runtime.Serialization;

namespace FeedService.Domain
{
    [DataContract]
    public class TopicItem
    {
        [DataMember]
        public string Name { get; set; }

        public ServicePartitionKey GetPartitionKey()
        {
            return new ServicePartitionKey(1);
        }
    }
}