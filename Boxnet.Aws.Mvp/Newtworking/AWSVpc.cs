using Amazon.EC2;
using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.Mvp.Newtworking
{
    public class AwsVpc
    {
        public ResourceIdWithAwsId Id { get; set; }
        public string CidrBlock { get; set; }
        public string Tenancy { get; set; }
        public List<AwsSubnet> Subnets { get; set; } = new List<AwsSubnet>();
        public List<AwsSecurityGroup> SecurityGroups { get; set; } = new List<AwsSecurityGroup>();
    }
}
