using Boxnet.Aws.Mvp.Iam;
using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.Mvp
{
    public class Stack
    {
        public IEnumerable<IamPolicy> IamPolicies { get; set; }
        public IEnumerable<IamRole> IamRoles { get; set; }
        public string Name { get; set; }
        public string Environment { get; set; }
    }
}
