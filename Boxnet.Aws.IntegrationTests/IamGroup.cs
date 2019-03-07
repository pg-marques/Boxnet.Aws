using System.Collections.Generic;

namespace Boxnet.Aws.IntegrationTests
{
    public class IamGroup : ResourceEntity<IamGroupId,IamGroupResourceId>
    {
        private readonly IList<IamAttachablePolicyResourceId> managedPolicies = new List<IamAttachablePolicyResourceId>();

        public string Path { get; }
        public IEnumerable<IamAttachablePolicyResourceId> AttachedPolicies { get { return managedPolicies; } }

        public IamGroup(IamGroupId id, IamGroupResourceId resourceId, string path) : base(id, resourceId)
        {
            Path = path;
        }

        public void Add(IamAttachablePolicyResourceId policyId)
        {
            managedPolicies.Add(policyId);
        }
    }
}
