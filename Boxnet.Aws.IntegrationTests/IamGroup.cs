using System.Collections.Generic;

namespace Boxnet.Aws.IntegrationTests
{
    public class IamGroup
    {
        private readonly IList<IamAttachablePolicyId> managedPolicies = new List<IamAttachablePolicyId>();

        public string Path { get; }
        public IamGroupId Id { get; private set; }
        public IEnumerable<IamAttachablePolicyId> AttachedPolicies { get { return managedPolicies; } }

        public IamGroup(IamGroupId id, string path)
        {
            Id = id;
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
