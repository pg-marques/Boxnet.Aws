using Amazon.Lambda;
using Amazon.Lambda.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.Mvp.Lambdas
{
    public class Lambda
    {
        public ResourceIdWithArn Id { get; set; }
        public string Description { get; set; }
        public DeadLetterConfig DeadLetterConfig { get; set; }
        public Amazon.Lambda.Model.Environment Environment { get; set; }
        public string Handler { get; set; }
        public string KMSKeyArn { get; set; }
        public List<string> Layers { get; set; }
        public int MemorySize { get; set; }
        public bool PublishOnCreation {get;set;}
        public string Role { get; set; }
        public Runtime Runtime { get; set; }
        public int Timeout { get; set; }
        public TracingConfig TracingConfig { get; set; }
        public VpcConfig VpcConfig { get; set; } 
        public DateTime? LastModifiedOnDestination { get; set; }
        public DateTime? LastModifiedOnSource { get; set; }
        public string Version { get; set; }
    }
}
