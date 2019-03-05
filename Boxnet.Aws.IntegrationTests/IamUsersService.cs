using Amazon;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Runtime;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Boxnet.Aws.IntegrationTests
{
    public class IamUsersService : IIamUsersDisposableService
    {
        private readonly AmazonIdentityManagementServiceClient client;

        public IamUsersService(string accessKeyId, string accessKey, string awsRegion)
        {
            client = new AmazonIdentityManagementServiceClient(new BasicAWSCredentials(accessKeyId, accessKey), RegionEndpoint.GetBySystemName(awsRegion));
        }

        public async Task<IEnumerable<IamUser>> ListByFilterAsync(IResourceIdFilter filter)
        {
            var details = await GetAccounstAuthorizationDetailsAsync(filter);

            return details.UsersDetails.ToIamUsersCollection();
        }

        private async Task<AccountsAuthorizationDetails> GetAccounstAuthorizationDetailsAsync(IResourceIdFilter filter)
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

        public async Task CreateAsync(IamUser user)
        {
            await CreateUserAsync(user);
            await AddToGroupsAsync(user);
        }

        private async Task CreateUserAsync(IamUser user)
        {
            var response = await client.CreateUserAsync(new CreateUserRequest()
            {
                UserName = user.Id.Name,
                Path = user.Path
            });

            user.SetArn(response.User.Arn);
        }

        private async Task AddToGroupsAsync(IamUser user)
        {
            foreach (var groupId in user.GroupsIds)
                await client.AddUserToGroupAsync(new AddUserToGroupRequest()
                {
                    UserName = user.Id.Name,
                    GroupName = groupId.Name
                });
        }

        public async Task DeleteAsync(IamUser user)
        {
            await RemoveFromGroupsAsync(user);
            await DeleteUserAsync(user);
        }

        private async Task RemoveFromGroupsAsync(IamUser user)
        {
            foreach (var groupId in user.GroupsIds)
                await client.RemoveUserFromGroupAsync(new RemoveUserFromGroupRequest()
                {
                    UserName = user.Id.Name,
                    GroupName = groupId.Name
                });
        }

        private async Task DeleteUserAsync(IamUser user)
        {
            var response = await client.DeleteUserAsync(new DeleteUserRequest()
            {
                UserName = user.Id.Name
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
