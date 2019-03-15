using Amazon.CloudWatchEvents;
using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.Mvp.CloudWatch
{
    public class CloudWatchRule
    {
        public ResourceIdWithArn Id { get; set; }
        public string Description { get; set; }
        public string EventPattern { get; set; }
        public string ScheduleExpression { get; set; }
        public RuleState State { get; set; }
    }
}
