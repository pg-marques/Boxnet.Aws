using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.Mvp.Sns
{
    public class SnsSubscription
    {
        public ResourceIdWithArn Id { get; set; }
        public Dictionary<string, string> Attributes { get; set; }
        public string Endpoint { get; set; }
        public string Protocol { get; set; }
        public bool ReturnSubscriptionArn { get; set; }
        public ResourceIdWithArn TopicId { get; set; }
    }
}
