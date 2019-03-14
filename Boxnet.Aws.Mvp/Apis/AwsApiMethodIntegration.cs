using Amazon.APIGateway;
using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.Mvp.Apis
{
    public class AwsApiMethodIntegration
    {
        public int TimeoutInMillis { get; set; }
        public ResourceIdWithAwsId RestApiId { get; set; }
        public ResourceId ResourceId { get; set; }
        public Dictionary<string, string> RequestTemplates { get; set; }
        public Dictionary<string, string> RequestParameters { get; set; }
        public string PassthroughBehavior { get; set; }
        public IntegrationType Type { get; set; }
        public string IntegrationHttpMethod { get; set; }
        public string Credentials { get; set; }
        public ContentHandlingStrategy ContentHandling { get; set; }
        public ConnectionType ConnectionType { get; set; }
        public string ConnectionId { get; set; }
        public List<string> CacheKeyParameters { get; set; }
        public string HttpMethod { get; set; }
        public string Uri { get; set; }
        public bool IsCreated { get; set; }
        public List<AwsApiMethodIntegrationResponse> Responses { get; set; } = new List<AwsApiMethodIntegrationResponse>();
    }
}
