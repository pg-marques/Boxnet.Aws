using Amazon;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Runtime;
using Boxnet.Aws.Model.Aws;
using Boxnet.Aws.Model.Aws.Iam.Groups;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Boxnet.Aws.Infra.Aws.Iam.Groups
{
    public class IamGroupsService : IIamGroupsDisposableService
    {
        private readonly AmazonIdentityManagementServiceClient client;

        public IamGroupsService(string accessKeyId, string accessKey, string awsRegion)
        {
            client = new AmazonIdentityManagementServiceClient(new BasicAWSCredentials(accessKeyId, accessKey), RegionEndpoint.GetBySystemName(awsRegion));
        }

        public async Task<IEnumerable<IamGroup>> ListByFilterAsync(IResourceIdFilter filter)
        {
            var details = await GetAccountsAuthorizationDetailsAsync(filter);

            return details.GroupsDetails.ToIamGroupsCollection();
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

        public async Task CreateAsync(IamGroup group)
        {
            var response = await client.CreateGroupAsync(new CreateGroupRequest()
            {
                GroupName = group.ResourceId.Name,
                Path = group.Path
            });

            group.SetArn(response.Group.Arn);
        }

        public async Task DeleteAsync(IamGroup group)
        {
            await client.DeleteGroupAsync(new DeleteGroupRequest()
            {
                GroupName = group.ResourceId.Name
            });
        }

        public async Task DetachPoliciesAsync(IamGroup group)
        {
            foreach (var policy in group.AttachedPolicies)
                await client.DetachGroupPolicyAsync(new DetachGroupPolicyRequest()
                {
                    PolicyArn = policy.Arn,
                    GroupName = group.ResourceId.Name
                });
        }

        public async Task AttachPoliciesAsync(IamGroup group)
        {
            foreach (var policy in group.AttachedPolicies)
                await client.AttachGroupPolicyAsync(new AttachGroupPolicyRequest()
                {
                    GroupName = group.ResourceId.Name,
                    PolicyArn = policy.Arn
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
