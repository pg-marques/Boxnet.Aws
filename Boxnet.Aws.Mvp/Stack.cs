using Boxnet.Aws.Mvp.Iam;
using Boxnet.Aws.Mvp.Newtworking;
using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.Mvp
{
    public class Stack
    {
        public string Name { get; set; }
        public string Environment { get; set; }
        public IEnumerable<IamPolicy> IamPolicies { get; set; }
        public IEnumerable<IamRole> IamRoles { get; set; }
        public IEnumerable<IamGroup> IamGroups { get; set; }
        public IEnumerable<IamUser> IamUsers { get; set; }
        public IEnumerable<AwsVpc> Vpcs { get; set; }
    }
}
