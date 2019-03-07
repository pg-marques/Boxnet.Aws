using System.Collections.Generic;

namespace Boxnet.Aws.IntegrationTests
{
    public class IamRole : ResourceEntity<IamRoleId,IamRoleResourceId>
    {
        private readonly IList<IamAttachablePolicyResourceId> attachedPoliciesIds = new List<IamAttachablePolicyResourceId>();
        private readonly IList<IamInlinePolicy> inlinePolicies = new List<IamInlinePolicy>();

        public string Path { get; }
        public string Description { get; }
        public int MaxSessionDuration { get; }
        public IIamPolicyDocument AssumeRolePolicyDocument { get; }

        public IEnumerable<IamAttachablePolicyResourceId> AttachedPoliciesIds { get { return attachedPoliciesIds; } }
        public IEnumerable<IamInlinePolicy> InlinePolicies { get { return inlinePolicies; } }

        public IamRole(IamRoleId id,  IamRoleResourceId resourceId,  string path,  string description,  int maxSessionDuration,  IIamPolicyDocument assumeRolePolicyDocument) : base(id, resourceId)
        {            
            Path = path;
            Description = description;
            MaxSessionDuration = maxSessionDuration;
            AssumeRolePolicyDocument = assumeRolePolicyDocument;
        }

        public void AddAttachedPolicyId(IamAttachablePolicyResourceId policyId)
        {
            attachedPoliciesIds.Add(policyId);
        }

        public void AddInlinePolicy(IamInlinePolicy policy)
        {
            inlinePolicies.Add(policy);
        }
    }
}
