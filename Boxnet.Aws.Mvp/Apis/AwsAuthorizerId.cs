using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.Mvp.Apis
{
    public class AwsAuthorizerId
    {
        public string PreviousId { get; set; }
        public string NewId { get; set; }
        public string PreviousName { get; set; }
        public string NewName { get; set; }
    }
}
