using Amazon.IdentityManagement.Model;
using Boxnet.Aws.Model.Aws;
using Boxnet.Aws.Model.Aws.Iam.Groups;
using Boxnet.Aws.Model.Aws.Iam.Policies;
using Boxnet.Aws.Model.Aws.Iam.Users;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Boxnet.Aws.Infra.Aws.Iam.Users
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
                filter.IsSatisfiedBy(new IamUserResourceId(user.UserName)) ||
                user.GroupList.Any(groupName => filter.IsSatisfiedBy(new IamGroupResourceId(groupName))) ||
                user.AttachedManagedPolicies.Any(policyDetail => filter.IsSatisfiedBy(new IamAttachablePolicyResourceId(policyDetail.PolicyName))));

            return new UsersDetails(filteredDetails, filter);
        }

        public IEnumerable<IamUser> ToIamUsersCollection()
        {
            return details.Select(detail =>
            {
                var user = new IamUser(new IamUserId(), new IamUserResourceId(detail.UserName, detail.Arn), detail.Path);

                foreach (var group in detail.GroupList.Where(groupName => filter.IsSatisfiedBy(new IamGroupResourceId(groupName))))
                    user.AddGroupId(new IamGroupResourceId(group));

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
