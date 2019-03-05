using Amazon;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Boxnet.Aws.IntegrationTests
{
    public class IamRolesService : IIamRolesDisposableService
    {
        private readonly AmazonIdentityManagementServiceClient client;

        public IamRolesService(string accessKeyId, string accessKey, string awsRegion)
        {
            client = new AmazonIdentityManagementServiceClient(new BasicAWSCredentials(accessKeyId, accessKey), RegionEndpoint.GetBySystemName(awsRegion));
        }

        public async Task<IEnumerable<IamRole>> ListByFilterAsync(IResourceIdFilter filter)
        {
            var roles = await GetRolesListAsync(filter);

            await AttachPoliciesAsync(roles);
            await AddInlinePoliciesAsync(roles);

            return roles;
        }

        private async Task<IEnumerable<IamRole>> GetRolesListAsync(IResourceIdFilter filter)
        {
            var details = await GetAccountsAuthorizationDetailsAsync(filter);

            var roles = new List<IamRole>();

            foreach (var roleDetails in details.RolesDetails)
            {
                var role = await GetRoleAsync(roleDetails);

                roles.Add(role);
            }

            return roles;
        }

        private async Task AttachPoliciesAsync(IEnumerable<IamRole> roles)
        {
            foreach (var role in roles)
            {
                var policies = await GetAttachablePoliciesIdsAsync(role);

                foreach (var policyId in policies)
                    role.AddAttachedPolicyId(policyId);
            }
        }

        private async Task AddInlinePoliciesAsync(IEnumerable<IamRole> roles)
        {
            foreach (var role in roles)
            {
                var policies = await GetInlinePoliciesAsync(role);

                foreach (var policy in policies)
                    role.AddInlinePolicy(policy);
            }
        }

        private async Task<IamRole> GetRoleAsync(RoleDetail roleDetails)
        {
            var response = await client.GetRoleAsync(new GetRoleRequest()
            {
                RoleName = roleDetails.RoleName
            });

            return new IamRole(
                new IamRoleId(roleDetails.Arn, roleDetails.RoleName),
                roleDetails.Path,
                response.Role.Description,
                response.Role.MaxSessionDuration,
                new IamPolicyUndecodedDocument(roleDetails.AssumeRolePolicyDocument));
        }

        private async Task<IEnumerable<IamAttachablePolicyId>> GetAttachablePoliciesIdsAsync(IamRole role)
        {
            var attachedPoliciesIds = new List<IamAttachablePolicyId>();

            string marker = null;
            do
            {
                var response = await client.ListAttachedRolePoliciesAsync(new ListAttachedRolePoliciesRequest()
                {
                    Marker = marker,
                    RoleName = role.Id.Name
                });

                attachedPoliciesIds.AddRange(
                    response.AttachedPolicies.Select(policy => new IamAttachablePolicyId(policy.PolicyArn, policy.PolicyName)));

                marker = response.Marker;

            } while (marker != null);

            return attachedPoliciesIds;
        }

        private async Task<IEnumerable<IamInlinePolicy>> GetInlinePoliciesAsync(IamRole role)
        {
            var inlinePolicies = new List<IamInlinePolicy>();
            var inlinePoliciesNames = await GetInlinePoliciesNamesAsync(role);            

            foreach (var policy in inlinePoliciesNames)
            {
                var response = await client.GetRolePolicyAsync(new GetRolePolicyRequest()
                {
                    PolicyName = policy,
                    RoleName = role.Id.Name
                });

                inlinePolicies.Add(new IamInlinePolicy(
                    new IamInlinePolicyId(policy),
                    new IamPolicyUndecodedDocument(response.PolicyDocument)));
            }

            return inlinePolicies;
        }

        private async Task<IEnumerable<string>> GetInlinePoliciesNamesAsync(IamRole role)
        {
            var inlinePoliciesNames = new List<string>();

            string marker = null;
            do
            {
                var response = await client.ListRolePoliciesAsync(new ListRolePoliciesRequest()
                {
                    Marker = marker,
                    RoleName = role.Id.Name
                });
                
                inlinePoliciesNames.AddRange(response.PolicyNames);

                marker = response.Marker;
            } while (marker != null);

            return inlinePoliciesNames;
        }

        private async Task<AccountsAuthorizationDetails> GetAccountsAuthorizationDetailsAsync(IResourceIdFilter filter)
        {
            var details = new AccountsAuthorizationDetails();

            string marker = null;
            do
            {
                var response = await client.GetAccountAuthorizationDetailsAsync(new GetAccountAuthorizationDetailsRequest()
                {
                    Marker = marker
                });
                
                details.Add(response);

                marker = response.Marker;

            } while (marker != null);

            return details.FilterBy(filter);
        }

        public async Task CreateAsync(IamRole role)
        {
            await CreateRoleAsync(role);
            await AttachPoliciesAsync(role);
            await AddInlinePoliciesAsync(role);
        }

        private async Task AddInlinePoliciesAsync(IamRole role)
        {
            foreach (var policy in role.InlinePolicies)
                await client.PutRolePolicyAsync(new PutRolePolicyRequest()
                {
                    RoleName = role.Id.Name,
                    PolicyName = policy.Id.Name,
                    PolicyDocument = policy.Document.Value
                });
        }

        private async Task AttachPoliciesAsync(IamRole role)
        {
            foreach (var policy in role.AttachedPoliciesIds)
                await client.AttachRolePolicyAsync(new AttachRolePolicyRequest()
                {
                    RoleName = role.Id.Name,
                    PolicyArn = policy.Arn
                });
        }

        private async Task CreateRoleAsync(IamRole role)
        {
            var response = await client.CreateRoleAsync(new CreateRoleRequest()
            {
                RoleName = role.Id.Name,
                Path = role.Path,
                Description = role.Description,
                MaxSessionDuration = role.MaxSessionDuration,
                AssumeRolePolicyDocument = role.AssumeRolePolicyDocument.Value                
            });

            role.SetArn(response.Role.Arn);
        }

        public async Task DeleteAsync(IamRole role)
        {
            await DetachPoliciesIdsAsync(role);
            await RemoveInlinePoliciesAsync(role);
            await DeleteRoleAsync(role);
        }

        private async Task DeleteRoleAsync(IamRole role)
        {
            await client.DeleteRoleAsync(new DeleteRoleRequest()
            {
                RoleName = role.Id.Name
            });
        }

        private async Task RemoveInlinePoliciesAsync(IamRole role)
        {
            foreach (var policy in role.InlinePolicies)
                await client.DeleteRolePolicyAsync(new DeleteRolePolicyRequest()
                {
                    PolicyName = policy.Id.Name,
                    RoleName = role.Id.Name
                });
        }

        private async Task DetachPoliciesIdsAsync(IamRole role)
        {
            foreach (var policy in role.AttachedPoliciesIds)
                await client.DetachRolePolicyAsync(new DetachRolePolicyRequest()
                {
                    PolicyArn = policy.Arn,
                    RoleName = role.Id.Name
                });
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    client.Dispose();
                }

                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
