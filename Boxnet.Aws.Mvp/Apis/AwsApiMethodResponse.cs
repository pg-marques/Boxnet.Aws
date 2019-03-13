using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.Mvp.Apis
{
    public class AwsApiMethodResponse
    {
        public string HttpMethod { get; set; }
        public ResourceId ResourceId { get; set; }
        public List<AwsApiModel> RequestModels { get; set; }
        public Dictionary<string, bool> ResponseParameters { get; set; }
        public ResourceIdWithAwsId RestApiId { get; set; }
        public string StatusCode { get; set; }
        public bool IsCreated { get; internal set; }
    }
}
