using Amazon.IdentityManagement.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Boxnet.Aws.IntegrationTests
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
            return new GroupsDetails(details.Where(group => filter.IsSatisfiedBy(new IamGroupId(group.GroupName))));
        }

        public IEnumerable<IamGroup> ToIamGroupsCollection()
        {
            return details.Select(detail =>
            {
                var group = new IamGroup(new IamGroupId(detail.Arn, detail.GroupName), detail.Path);

                foreach (var policy in detail.AttachedManagedPolicies)
                    group.Add(new IamAttachablePolicyId(policy.PolicyName, policy.PolicyArn));

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
