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
        public List<string> BinaryMediaTypes { get; set; }
        public ApiKeySourceType ApiKeySource { get; set; }
        public string Description { get; set; }
        public EndpointConfiguration EndpointConfiguration { get; set; }
        public int MinimumCompressionSize { get; set; }
        public string Policy { get; set; }
        public string Version { get; set; }
        public IEnumerable<AwsApiResource> Resources { get; set; } = new List<AwsApiResource>();
        public AwsApiResource RootResource
        {
            get
            {
                return Resources?.FirstOrDefault(resource => resource.ParentId.PreviousName == null && resource.PathPart == null && resource.Depth == 0);
            }
        }
    }
}
