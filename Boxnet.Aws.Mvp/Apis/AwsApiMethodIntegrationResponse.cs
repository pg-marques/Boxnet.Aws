using Amazon.APIGateway;
using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.Mvp.Apis
{
    public class AwsApiMethodIntegrationResponse
    {
        public ContentHandlingStrategy ContentHandling { get; set; }
        public string HttpMethod { get; set; }
        public ResourceId ResourceId { get; set; }
        public Dictionary<string, string> ResponseParameters { get; set; }
        public Dictionary<string, string> ResponseTemplates { get; set; }
        public ResourceIdWithAwsId RestApiId { get; set; }
        public string SelectionPattern { get; set; }
        public string StatusCode { get; set; }
        public bool Created { get; internal set; }
    }
}
