using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.Mvp.Sqs
{
    public class SqsQueue
    {
        public ResourceIdWithArn Id { get; set; }
        public Dictionary<string, string> Attributes { get; set; }
    }
}
