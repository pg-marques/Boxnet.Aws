using Amazon.IdentityManagement.Model;
using Boxnet.Aws.Model.Aws;
using Boxnet.Aws.Model.Aws.Iam.Groups;
using Boxnet.Aws.Model.Aws.Iam.Policies;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Boxnet.Aws.Infra.Aws.Iam.Groups
{
    public class GroupsDetails : IEnumerable<GroupDetail>
    {
        private readonly IEnumerable<GroupDetail> details = new List<GroupDetail>();

        public GroupsDetails(IEnumerable<GroupDetail> details)
        {
            this.details = details;
        }

        public GroupsDetails FilterBy(IResourceIdFilter filter)
        {
            return new GroupsDetails(details.Where(group => filter.IsSatisfiedBy(new IamGroupResourceId(group.GroupName))));
        }

        public IEnumerable<IamGroup> ToIamGroupsCollection()
        {
            return details.Select(detail =>
            {
                var group = new IamGroup(new IamGroupId(), new IamGroupResourceId(detail.GroupName, detail.Arn), detail.Path);

                foreach (var policy in detail.AttachedManagedPolicies)
                    group.AddAttachedPolicyId(new IamAttachablePolicyResourceId(policy.PolicyName, policy.PolicyArn));

                return group;
            });
        }

        public IEnumerator<GroupDetail> GetEnumerator()
        {
            return details.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return details.GetEnumerator();
        }
    }
}
