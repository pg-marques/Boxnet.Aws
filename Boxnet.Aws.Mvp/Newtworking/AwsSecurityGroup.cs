using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.Mvp.Newtworking
{
    public class AwsSecurityGroup
    {
        public ResourceIdWithAwsId Id { get; set; }
        public ResourceIdWithAwsId VpcId { get; set; }
        public string Description { get; set; }
    }
}
