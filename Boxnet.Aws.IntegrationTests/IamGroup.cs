using System.Collections.Generic;

namespace Boxnet.Aws.IntegrationTests
{
    public class IamGroup : Entity<IamGroupId>, IResource<IamGroupId>
    {
        private readonly IList<IamAttachablePolicyId> managedPolicies = new List<IamAttachablePolicyId>();

        public string Path { get; }
        public IEnumerable<IamAttachablePolicyId> AttachedPolicies { get { return managedPolicies; } }

        public IamGroup(IamGroupId id, string path) : base(id)
        {
            Path = path;
        }

        public void Add(IamAttachablePolicyId policyId)
        {
            managedPolicies.Add(policyId);
        }

        public void SetArn(string arn)
        {
            Id.SetArn(arn);
        }
    }
}
