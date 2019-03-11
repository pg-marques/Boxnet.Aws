using Amazon.Lambda;
using Amazon.Lambda.Model;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.Mvp.Lambdas
{
    public class Lambda
    {
        public ResourceIdWithArn Id { get; set; }
        public string Description { get; set; }
        public DeadLetterConfig DeadLetterConfig { get; set; }
        public Environment Environment { get; set; }
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
    }
}
