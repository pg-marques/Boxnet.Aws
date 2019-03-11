using System;
using System.Collections.Generic;
using System.Text;
using Amazon.IdentityManagement.Model;

namespace Boxnet.Aws.Mvp.Iam
{
    public class IamRole
    {
        public ResourceIdWithArn Id { get; set; }
        public string AssumeRolePolicyDocument { get; set; }
        public string Description { get; set; }
        public int MaxSessionDuration { get; set; }
        public string Path { get; set; }
        public AttachedPermissionsBoundary PermissionsBoundary { get; set; }
        public IEnumerable<ResourceIdWithArn> AttachedPoliciesIds { get; set; }
        public IEnumerable<IamInlinePolicy> InlinePolicies { get; set; }
    }
}
