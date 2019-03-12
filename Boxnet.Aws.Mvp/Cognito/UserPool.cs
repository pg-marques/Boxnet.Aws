using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.Mvp.Cognito
{
    public class UserPool
    {
        public UserPoolId Id { get; set; }
        public UserPoolAddOnsType UserPoolAddOns { get; set; }
        public List<string> UsernameAttributes { get; set; }
        public string SmsVerificationMessage { get; set; }
        public SmsConfigurationType SmsConfiguration { get; set; }
        public string SmsAuthenticationMessage { get; set; }
        public List<SchemaAttributeType> Schema { get; set; }
        public UserPoolPolicyType Policies { get; set; }
        public UserPoolMfaType MfaConfiguration { get; set; }
        public LambdaConfigType LambdaConfig { get; set; }
        public string EmailVerificationSubject { get; set; }
        public string EmailVerificationMessage { get; set; }
        public EmailConfigurationType EmailConfiguration { get; set; }
        public DeviceConfigurationType DeviceConfiguration { get; set; }
        public List<string> AutoVerifiedAttributes { get; set; }
        public List<string> AliasAttributes { get; set; }
        public AdminCreateUserConfigType AdminCreateUserConfig { get; set; }
        public VerificationMessageTemplateType VerificationMessageTemplate { get; set; }
    }
}
