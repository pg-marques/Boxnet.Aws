using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.Mvp.Newtworking
{
    public class AwsSubnet
    {
        public AwsSubnetId Id { get; set; }
        public ResourceIdWithAwsId VpcId { get; set; }
        public string AvailabilityZone { get; set; }
        public string AvailabilityZoneId { get; set; }
        public string CidrBlock { get; set; }
    }
}
