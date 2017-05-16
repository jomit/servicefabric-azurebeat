using FeedService.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserService.Models
{
    public class UserSubscription
    {
        public string Email { get; set; }

        public List<FeedItem> Feeds { get; set; }
    }
}
