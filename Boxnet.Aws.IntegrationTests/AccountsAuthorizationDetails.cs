using Amazon.IdentityManagement.Model;
using System.Collections.Generic;
using System.Linq;

namespace Boxnet.Aws.IntegrationTests
{
    public class AccountsAuthorizationDetails
    {
        private readonly IResourceIdFilter filter;
        private readonly List<GetAccountAuthorizationDetailsResponse> responses;

        public GroupsDetails GroupsDetails
        {
            get { return new GroupsDetails(responses.SelectMany(response => response.GroupDetailList)).FilterBy(filter); }
        }

        public RolesDetails RolesDetails
        {
            get { return new RolesDetails(responses.SelectMany(response => response.RoleDetailList)).FilterBy(filter); }
        }

        public UsersDetails UsersDetails
        {
            get
            {
                return new UsersDetails(responses.SelectMany(response => response.UserDetailList)).FilterBy(filter);
            }
        }

        public IEnumerable<string> PoliciesArns
        {
            get
            {
                var policiesArns = new List<string>();
                foreach (var response in responses)
                {
                    foreach (var policy in response.Policies.Where(policy => filter.IsSatisfiedBy(new IamAttachablePolicyResourceId(policy.PolicyName))))
                        policiesArns.Add(policy.Arn);

                    foreach (var policy in response.UserDetailList.Where(user => filter.IsSatisfiedBy(new IamUserResourceId(user.UserName))).SelectMany(user => user.AttachedManagedPolicies))
                        policiesArns.Add(policy.PolicyArn);

                    foreach (var policy in response.GroupDetailList.Where(group => filter.IsSatisfiedBy(new IamGroupResourceId(group.GroupName))).SelectMany(group => group.AttachedManagedPolicies))
                        policiesArns.Add(policy.PolicyArn);

                    foreach (var policy in response.RoleDetailList.Where(role => filter.IsSatisfiedBy(new IamRoleResourceId(role.RoleName))).SelectMany(role => role.AttachedManagedPolicies))
                        policiesArns.Add(policy.PolicyArn);
                }

                return policiesArns.Distinct();
            }
        }

        public AccountsAuthorizationDetails()
            : this(Enumerable.Empty<GetAccountAuthorizationDetailsResponse>(), new AlwaysTrueFilter()) { }

        private AccountsAuthorizationDetails(IEnumerable<GetAccountAuthorizationDetailsResponse> responses, IResourceIdFilter filter)
        {
            this.responses = responses.ToList();
            this.filter = filter;
        }

        public void Add(GetAccountAuthorizationDetailsResponse response)
        {
            responses.Add(response);
        }

        public AccountsAuthorizationDetails FilterBy(IResourceIdFilter filter)
        {
            return new AccountsAuthorizationDetails(responses, filter);
        }

    }
}
