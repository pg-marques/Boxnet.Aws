using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.APIGateway;
using Amazon.APIGateway.Model;

namespace Boxnet.Aws.Mvp.Apis
{
    public class AwsApi
    {
        public ResourceIdWithAwsId Id { get; set; }
        public List<string> BinaryMediaTypes { get; set; } = new List<string>();
        public ApiKeySourceType ApiKeySource { get; set; }
        public string Description { get; set; }
        public EndpointConfiguration EndpointConfiguration { get; set; }
        public int MinimumCompressionSize { get; set; }
        public string Policy { get; set; }
        public string Version { get; set; }
        public AwsApiResource RootResource { get; set; }
        public List<AwsApiModel> Models { get; set; } = new List<AwsApiModel>();
    }
}
