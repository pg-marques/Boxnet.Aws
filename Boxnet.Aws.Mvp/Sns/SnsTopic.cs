using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.Mvp.Sns
{
    public class SnsTopic
    {
        public ResourceIdWithArn Id { get; set; }
        public List<SnsSubscription> Subscriptions { get; set; } = new List<SnsSubscription>();
        public Dictionary<string, string> Attributes { get; set; }
    }
}
