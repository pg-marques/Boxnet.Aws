using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.Mvp.Apis
{
    public class AwsApiMethod
    {
        public string Verb { get; set; }        
        public ResourceId ResourceId { get; set; }
        public ResourceIdWithAwsId RestApiId { get; set; }
        public bool ApiKeyRequired { get; set; }
        public List<string> AuthorizationScopes { get; set; }
        public string AuthorizationType { get; set; }
        public string AuthorizerId { get; set; }
        public string OperationName { get; set; }
        public List<AwsApiModel> RequestModels { get; set; }
        public Dictionary<string, bool> RequestParameters { get; set; }
        public string RequestValidatorId { get; set; }
        public List<AwsApiMethodResponse> Responses { get; set; } = new List<AwsApiMethodResponse>();
        public AwsApiMethodIntegration Integration { get; set; }
    }
}
