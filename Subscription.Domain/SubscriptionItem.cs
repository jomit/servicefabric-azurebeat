using FeedService.Domain;
using Microsoft.ServiceFabric.Services.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Subscription.Domain
{
    [DataContract]
    public class SubscriptionItem
    {
        [DataMember]
        public string UserEmail { get; set; }

        [DataMember]
        public List<FeedItem> Feeds { get; set; }


        public ServicePartitionKey GetPartitionKey()
        {
            //Simple sharding for 4 partitions based on first letter of the email
            var firstLetterKey = System.Convert.ToInt32(this.UserEmail.Trim().ToLower().ToCharArray()[0]);
            if (firstLetterKey >= 97 && firstLetterKey <= 102)
            {
                return new ServicePartitionKey(1);
            }
            else if (firstLetterKey >= 103 && firstLetterKey <= 108)
            {
                return new ServicePartitionKey(2);
            }
            else if (firstLetterKey >= 109 && firstLetterKey <= 114)
            {
                return new ServicePartitionKey(3);
            }
            else
            {
                return new ServicePartitionKey(4);
            }
        }
    }
}
