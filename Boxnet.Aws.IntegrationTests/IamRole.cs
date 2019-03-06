using System.Collections.Generic;

namespace Boxnet.Aws.IntegrationTests
{
    public class IamRole : Entity<IamRoleId>, IResource<IamRoleId>
    {
        private readonly IList<IamAttachablePolicyId> attachedPoliciesIds = new List<IamAttachablePolicyId>();
        private readonly IList<IamInlinePolicy> inlinePolicies = new List<IamInlinePolicy>();

        public string Path { get; }
        public string Description { get; }
        public int MaxSessionDuration { get; }
        public IIamPolicyDocument AssumeRolePolicyDocument { get; }

        public IEnumerable<IamAttachablePolicyId> AttachedPoliciesIds { get { return attachedPoliciesIds; } }
        public IEnumerable<IamInlinePolicy> InlinePolicies { get { return inlinePolicies; } }

        public IamRole(IamRoleId id, string path, string description, int maxSessionDuration, IIamPolicyDocument assumeRolePolicyDocument) : base(id)
        {
            Path = path;
            Description = description;
            MaxSessionDuration = maxSessionDuration;
            AssumeRolePolicyDocument = assumeRolePolicyDocument;
        }

        public void AddAttachedPolicyId(IamAttachablePolicyId policyId)
        {
            attachedPoliciesIds.Add(policyId);
        }

        public void AddInlinePolicy(IamInlinePolicy policy)
        {
            inlinePolicies.Add(policy);
        }

        public void SetArn(string arn)
        {
            Id.SetArn(arn);
        }
    }
}
