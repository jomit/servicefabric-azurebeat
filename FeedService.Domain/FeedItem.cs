using Microsoft.ServiceFabric.Services.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FeedService.Domain
{
    [DataContract]
    public class FeedItem
    {
        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public Uri Url { get; set; }

        [DataMember]
        public List<TopicItem> Topics { get; set; }

        [DataMember]
        public List<PostItem> Posts { get; set; }

        [DataMember]
        public DateTimeOffset LastUpdatedTimeStamp { get; set; }

        [DataMember]
        public bool DisableSync { get; set; }

        public string GetUrlHash()
        {
            var hashText = GetMD5Hash();
            var builder = new StringBuilder();
            for (int i = 0; i < hashText.Length; i++)
            {
                builder.Append(hashText[i].ToString("X2"));
            }
            return builder.ToString();
        }

        public ServicePartitionKey GetPartitionKey()
        {
            int partitionSize = 4;
            var hashText = GetMD5Hash();  //Do not use GetHashCode()
            var rawParititionKey = BitConverter.ToInt64(hashText, 0) ^ BitConverter.ToInt64(hashText, 7);
            var partitionBucketKey = rawParititionKey % partitionSize;
            return new ServicePartitionKey((partitionBucketKey < 0 ? partitionBucketKey * -1 : partitionBucketKey) + 1);
        }

        public byte[] GetMD5Hash()
        {
            var byteContents = Encoding.Unicode.GetBytes(Url.ToString());
            return new MD5CryptoServiceProvider().ComputeHash(byteContents);
        }
    }
}
