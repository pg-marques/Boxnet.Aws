using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.Mvp.Cognito
{
    public class UserPoolId : ResourceIdWithArn
    {
        public string PreviousId { get; set; }
        public string NewId { get; set; }
    }
}
