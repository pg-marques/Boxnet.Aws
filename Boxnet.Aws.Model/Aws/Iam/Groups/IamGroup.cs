using Boxnet.Aws.Model.Aws.Iam.Policies;
using System.Collections.Generic;

namespace Boxnet.Aws.Model.Aws.Iam.Groups
{
    public class IamGroup : ResourceEntity<IamGroupId, IamGroupResourceId>
    {
        private readonly IList<IamAttachablePolicyResourceId> attachedPolicies = new List<IamAttachablePolicyResourceId>();

        public string Path { get; }
        public IEnumerable<IamAttachablePolicyResourceId> AttachedPolicies { get { return attachedPolicies; } }

        public IamGroup(IamGroupId id, IamGroupResourceId resourceId, string path) : base(id, resourceId)
        {
            Path = path;
        }

        public void AddAttachedPolicyId(IamAttachablePolicyResourceId policyId)
        {
            attachedPolicies.Add(policyId);
        }

        public void AddAttachedPoliciesIds(IEnumerable<IamAttachablePolicyResourceId> policiesIds)
        {
            foreach(var policyId in policiesIds)
                AddAttachedPolicyId(policyId);
        }
    }
}
