using Amazon.APIGateway;
using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.Mvp.Apis
{
    public class AwsApiAuthorizer
    {
        public AwsAuthorizerId Id { get; set; }
        public string AuthorizerCredentials { get; set; }
        public int AuthorizerResultTtlInSeconds { get; set; }
        public string AuthorizerUri { get; set; }
        public string AuthType { get; set; }
        public string IdentitySource { get; set; }
        public string IdentityValidationExpression { get; set; }
        public List<string> ProviderARNs { get; set; } = new List<string>();
        public ResourceIdWithAwsId RestApiId { get; set; }
        public AuthorizerType Type { get; set; }
    }
}
