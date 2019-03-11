using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.Mvp.Newtworking
{
    public class AwsSubnetId
    {
        public string PreviousName { get; set; }
        public string PreviousId { get; set; }
        public string PreviousArn { get; set; }
        public string NewName { get; set; }
        public string NewId { get; set; }
        public string NewArn { get; set; }
    }
}
