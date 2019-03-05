using Amazon.IdentityManagement.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Boxnet.Aws.IntegrationTests
{

    public class UsersDetails : IEnumerable<UserDetail>
    {
        private readonly IEnumerable<UserDetail> details = new List<UserDetail>();
        private readonly IResourceIdFilter filter;

        public UsersDetails(IEnumerable<UserDetail> details)
            : this(details, new AlwaysTrueFilter()) { }

        private UsersDetails(IEnumerable<UserDetail> details, IResourceIdFilter filter)
        {
            this.details = details;
            this.filter = filter;
        }

        public UsersDetails FilterBy(IResourceIdFilter filter)
        {
            var filteredDetails = details.Where(user => 
                filter.IsSatisfiedBy(new IamUserId(user.UserName)) || 
                user.GroupList.Any(groupName => filter.IsSatisfiedBy(new IamGroupId(groupName))) ||
                user.AttachedManagedPolicies.Any(policyDetail => filter.IsSatisfiedBy(new IamAttachablePolicyId(policyDetail.PolicyName))));

            return new UsersDetails(filteredDetails, filter);
        }

        public IEnumerable<IamUser> ToIamUsersCollection()
        {
            return details.Select(detail =>
            {
                var user = new IamUser(new IamUserId(detail.Arn, detail.UserName), detail.Path);

                foreach (var group in detail.GroupList.Where(groupName => filter.IsSatisfiedBy(new IamGroupId(groupName))))
                    user.AddGroupId(new IamGroupId(group));

                return user;
            });
        }

        public IEnumerator<UserDetail> GetEnumerator()
        {
            return details.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return details.GetEnumerator();
        }
    }
}
